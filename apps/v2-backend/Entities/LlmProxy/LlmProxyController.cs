using System.Text.Json;

namespace EnterpriseAgentOs.Api.Entities.LlmProxy;

/// <summary>
/// OpenAI-compatible <c>POST /v1/chat/completions</c> endpoint that proxies
/// LLM calls for agent pods. The agent authenticates with its per-agent
/// bearer token (<c>ZEROCLAW_SKILLS_BACKEND_TOKEN</c>). The backend
/// resolves the real provider + API key from the agent's record + the
/// global Providers table, then dispatches to the upstream provider.
///
/// This decouples agents from provider config: keys, providers, and models
/// can be changed in the dashboard without restarting pods. The agent never
/// holds the real API key.
/// </summary>
[ApiController]
[AgentTokenAuth]
public sealed class LlmProxyController : ControllerBase
{
    private readonly IAgentService _agents;
    private readonly IProviderService _providers;
    private readonly LlmProviderDispatcher _dispatcher;
    private readonly ILogger<LlmProxyController> _logger;

    public LlmProxyController(
        IAgentService agents,
        IProviderService providers,
        LlmProviderDispatcher dispatcher,
        ILogger<LlmProxyController> logger)
    {
        _agents = agents;
        _providers = providers;
        _dispatcher = dispatcher;
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

        // 3. Resolve provider + key + model from agent record
        var provider = agent.Provider;
        var model = agent.Model ?? "gpt-4o";

        if (!_dispatcher.IsSupported(provider))
        {
            HttpContext.Response.StatusCode = StatusCodes.Status422UnprocessableEntity;
            await HttpContext.Response.WriteAsJsonAsync(new { error = $"Provider '{provider}' is not supported by the LLM proxy." }, ct);
            return;
        }

        var apiKey = await _providers.GetDecryptedKeyAsync(provider, ct);
        if (apiKey is null)
        {
            HttpContext.Response.StatusCode = StatusCodes.Status409Conflict;
            await HttpContext.Response.WriteAsJsonAsync(new { error = $"Provider '{provider}' is not configured. Set its API key on the Providers page." }, ct);
            return;
        }

        // 4. Dispatch to upstream
        HttpResponseMessage upstream;
        try
        {
            upstream = await _dispatcher.DispatchAsync(provider, apiKey, model, body, ct);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "LLM proxy dispatch failed for agent {AgentId} provider {Provider}", agentId, provider);
            HttpContext.Response.StatusCode = StatusCodes.Status502BadGateway;
            await HttpContext.Response.WriteAsJsonAsync(new { error = $"Upstream error: {ex.Message}" }, ct);
            return;
        }

        // 5. Stream response back
        HttpContext.Response.StatusCode = (int)upstream.StatusCode;
        var contentType = upstream.Content.Headers.ContentType?.ToString() ?? "text/event-stream";
        HttpContext.Response.ContentType = contentType;

        await using var upstreamBody = await upstream.Content.ReadAsStreamAsync(ct);
        await upstreamBody.CopyToAsync(HttpContext.Response.Body, ct);
        upstream.Dispose();
    }
}
