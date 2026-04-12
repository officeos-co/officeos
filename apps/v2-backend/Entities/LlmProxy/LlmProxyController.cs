using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using EnterpriseAgentOs.Api.Properties;

namespace EnterpriseAgentOs.Api.Entities.LlmProxy;

/// <summary>
/// OpenAI-compatible <c>POST /v1/chat/completions</c> endpoint that proxies
/// LLM calls for agent pods. The agent authenticates with its per-agent
/// bearer token (<c>ZEROCLAW_SKILLS_BACKEND_TOKEN</c>). The backend
/// resolves the real provider + model from the agent's record, then:
/// <list type="bullet">
///   <item>OpenAI with a configured BYOK key → dispatches via <see cref="LlmProviderDispatcher"/>.</item>
///   <item>All other providers → forwards to the LiteLLM sidecar which holds platform keys.</item>
/// </list>
/// Credentials never leave the backend / LiteLLM sidecar.
/// </summary>
[ApiController]
[AgentTokenAuth]
public sealed class LlmProxyController : ControllerBase
{
    private readonly IAgentService _agents;
    private readonly IProviderService _providers;
    private readonly LlmProviderDispatcher _dispatcher;
    private readonly LiteLlmConfig _liteLlm;
    private readonly IHttpClientFactory _httpFactory;
    private readonly ILogger<LlmProxyController> _logger;

    public LlmProxyController(
        IAgentService agents,
        IProviderService providers,
        LlmProviderDispatcher dispatcher,
        LiteLlmConfig liteLlm,
        IHttpClientFactory httpFactory,
        ILogger<LlmProxyController> logger)
    {
        _agents = agents;
        _providers = providers;
        _dispatcher = dispatcher;
        _liteLlm = liteLlm;
        _httpFactory = httpFactory;
        _logger = logger;
    }

    [HttpPost("/v1/chat/completions")]
    public async Task ChatCompletions(CancellationToken ct)
    {
        // 1. Resolve agent from bearer token (set by AgentTokenAuth filter)
        if (!HttpContext.Items.TryGetValue("agent-id", out var agentIdObj) || agentIdObj is not Guid agentId)
        {
            HttpContext.Response.StatusCode = StatusCodes.Status401Unauthorized;
            await HttpContext.Response.WriteAsJsonAsync(new { error = "Agent not resolved" }, ct);
            return;
        }

        var agent = await _agents.GetAsync(agentId, ct);
        if (agent is null)
        {
            _logger.LogWarning("LLM proxy: agent {AgentId} not found", agentId);
            HttpContext.Response.StatusCode = StatusCodes.Status404NotFound;
            await HttpContext.Response.WriteAsJsonAsync(new { error = "Agent not found" }, ct);
            return;
        }

        // 2. Parse request body
        JsonElement body;
        try
        {
            using var doc = await JsonDocument.ParseAsync(HttpContext.Request.Body, cancellationToken: ct);
            body = doc.RootElement.Clone();
        }
        catch (JsonException ex)
        {
            HttpContext.Response.StatusCode = StatusCodes.Status400BadRequest;
            await HttpContext.Response.WriteAsJsonAsync(new { error = $"Invalid JSON: {ex.Message}" }, ct);
            return;
        }

        // 3. Resolve concrete model via SmartRouter
        var requestedModel = agent.Model ?? "auto";
        var provider = agent.Provider ?? "anthropic";

        // Map provider name to SmartRouter family
        var family = provider.ToLowerInvariant() switch
        {
            "openai" => "openai",
            "google" => "google",
            _ => "anthropic",
        };

        var resolvedModel = SmartRouter.Resolve(requestedModel, body, family);

        _logger.LogInformation(
            "LLM proxy: agent {AgentId} requested {RequestedModel} → resolved {ResolvedModel} (provider={Provider})",
            agentId, requestedModel, resolvedModel, provider);

        // 4. OpenAI BYOK path (kept for dev/testing)
        if (provider.Equals("openai", StringComparison.OrdinalIgnoreCase))
        {
            var apiKey = await _providers.GetDecryptedKeyAsync("openai", ct);
            if (apiKey is not null)
            {
                await DispatchViaByokAsync(provider, apiKey, resolvedModel, body, ct);
                return;
            }
            // Fall through to LiteLLM if no BYOK key configured
        }

        // 5. All other providers → LiteLLM sidecar
        await DispatchViaLiteLlmAsync(resolvedModel, body, ct);
    }

    // ── BYOK path (OpenAI only) ───────────────────────────────────────────────

    private async Task DispatchViaByokAsync(
        string provider,
        string apiKey,
        string model,
        JsonElement body,
        CancellationToken ct)
    {
        HttpResponseMessage upstream;
        try
        {
            upstream = await _dispatcher.DispatchAsync(provider, apiKey, model, body, ct);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "LLM proxy BYOK dispatch failed for provider {Provider}", provider);
            HttpContext.Response.StatusCode = StatusCodes.Status502BadGateway;
            await HttpContext.Response.WriteAsJsonAsync(new { error = $"Upstream error: {ex.Message}" }, ct);
            return;
        }

        await StreamUpstreamResponseAsync(upstream, ct);
    }

    // ── LiteLLM path ──────────────────────────────────────────────────────────

    private async Task DispatchViaLiteLlmAsync(string model, JsonElement body, CancellationToken ct)
    {
        // Replace model in the body, keep everything else intact
        var dict = JsonSerializer.Deserialize<Dictionary<string, JsonElement>>(body.GetRawText())
            ?? new Dictionary<string, JsonElement>();
        dict["model"] = JsonDocument.Parse($"\"{EscapeJson(model)}\"").RootElement.Clone();

        var json = JsonSerializer.Serialize(dict);
        var client = _httpFactory.CreateClient("llm-proxy");

        var url = $"{_liteLlm.BaseUrl.TrimEnd('/')}/v1/chat/completions";
        var req = new HttpRequestMessage(HttpMethod.Post, url)
        {
            Content = new StringContent(json, Encoding.UTF8, "application/json"),
        };
        // LiteLLM accepts any bearer when no master key is set; using a well-known
        // platform sentinel keeps the logs clean and allows future master-key adoption.
        req.Headers.Authorization = new AuthenticationHeaderValue("Bearer", "sk-platform");
        req.Headers.Accept.ParseAdd("text/event-stream");

        HttpResponseMessage upstream;
        try
        {
            upstream = await client.SendAsync(req, HttpCompletionOption.ResponseHeadersRead, ct);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "LLM proxy: LiteLLM call failed (url={Url} model={Model})", url, model);
            HttpContext.Response.StatusCode = StatusCodes.Status502BadGateway;
            await HttpContext.Response.WriteAsJsonAsync(new { error = $"LiteLLM upstream error: {ex.Message}" }, ct);
            return;
        }

        await StreamUpstreamResponseAsync(upstream, ct);
    }

    // ── shared response streaming ─────────────────────────────────────────────

    private async Task StreamUpstreamResponseAsync(HttpResponseMessage upstream, CancellationToken ct)
    {
        HttpContext.Response.StatusCode = (int)upstream.StatusCode;
        var contentType = upstream.Content.Headers.ContentType?.ToString() ?? "text/event-stream";
        HttpContext.Response.ContentType = contentType;

        await using var upstreamBody = await upstream.Content.ReadAsStreamAsync(ct);
        await upstreamBody.CopyToAsync(HttpContext.Response.Body, ct);
        upstream.Dispose();
    }

    private static string EscapeJson(string s) =>
        s.Replace("\\", "\\\\").Replace("\"", "\\\"");
}
