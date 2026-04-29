namespace EnterpriseAgentOs.Infrastructure.Features.Agents;

/// <summary>
/// Routes an OpenAI-compatible chat-completions request to the real upstream
/// provider, injecting the real API key. For OpenAI-compatible providers
/// this is a straight passthrough with key + model swap. For Anthropic,
/// the request/response format is translated on the fly.
/// </summary>
public sealed class LlmProviderDispatcher
{
    private static readonly Dictionary<string, string> OpenAiCompatBaseUrls =
        new(StringComparer.OrdinalIgnoreCase)
        {
            ["openai"] = "https://api.openai.com/v1",
            ["groq"] = "https://api.groq.com/openai/v1",
            ["deepseek"] = "https://api.deepseek.com/v1",
            ["xai"] = "https://api.x.ai/v1",
            ["openrouter"] = "https://openrouter.ai/api/v1",
            ["google"] = "https://generativelanguage.googleapis.com/v1beta/openai",
        };

    private readonly IHttpClientFactory _httpClientFactory;
    private readonly ILogger<LlmProviderDispatcher> _logger;

    public LlmProviderDispatcher(
        IHttpClientFactory httpFactory,
        ILogger<LlmProviderDispatcher> logger)
    {
        _httpClientFactory = httpFactory;
        _logger = logger;
    }

    public bool IsSupported(string provider) =>
        OpenAiCompatBaseUrls.ContainsKey(provider)
        || provider.Equals("anthropic", StringComparison.OrdinalIgnoreCase);

    /// <summary>
    /// Dispatch a chat-completions request to the upstream provider.
    /// Returns a streaming <see cref="HttpResponseMessage"/> whose body
    /// is standard OpenAI SSE (<c>data: {...}\n\n</c> lines).
    /// </summary>
    public async Task<AgentResult<HttpResponseMessage>> DispatchAsync(
        string provider,
        string apiKey,
        string model,
        JsonElement requestBody,
        CancellationToken ct)
    {
        _logger.LogInformation("Dispatching LLM request to {Provider} model {Model}", provider, model);

        try
        {
            if (provider.Equals("anthropic", StringComparison.OrdinalIgnoreCase))
            {
                return await DispatchAnthropicAsync(apiKey, model, requestBody, ct);
            }

            if (!OpenAiCompatBaseUrls.TryGetValue(provider, out var baseUrl))
            {
                return new AgentError(AgentErrorCategory.Configuration, $"Unsupported provider: {provider}");
            }

            return await DispatchOpenAiCompatAsync(baseUrl, apiKey, model, requestBody, ct);
        }
        catch (TaskCanceledException ex)
        {
            return new AgentError(AgentErrorCategory.LlmCall, "LLM call timed out", ex.Message);
        }
        catch (HttpRequestException ex)
        {
            return new AgentError(AgentErrorCategory.LlmCall, $"LLM call failed: {ex.Message}", ex.ToString());
        }
        catch (Exception ex)
        {
            return new AgentError(AgentErrorCategory.LlmCall, $"Unexpected LLM error: {ex.Message}", ex.ToString());
        }
    }

    private async Task<AgentResult<HttpResponseMessage>> DispatchOpenAiCompatAsync(
        string baseUrl,
        string apiKey,
        string model,
        JsonElement requestBody,
        CancellationToken ct)
    {
        // Clone the request, replace model
        var dict = JsonSerializer.Deserialize<Dictionary<string, JsonElement>>(requestBody.GetRawText())
            ?? new Dictionary<string, JsonElement>();
        dict["model"] = JsonDocument.Parse($"\"{EscapeJson(model)}\"").RootElement.Clone();

        var json = JsonSerializer.Serialize(dict);
        var client = _httpClientFactory.CreateClient("llm-proxy");
        var req = new HttpRequestMessage(HttpMethod.Post, $"{baseUrl.TrimEnd('/')}/chat/completions")
        {
            Content = new StringContent(json, Encoding.UTF8, "application/json"),
        };
        req.Headers.Authorization = new AuthenticationHeaderValue("Bearer", apiKey);
        req.Headers.Accept.ParseAdd("text/event-stream");

        var response = await client.SendAsync(req, HttpCompletionOption.ResponseHeadersRead, ct);
        if (!response.IsSuccessStatusCode)
        {
            var errorBody = await response.Content.ReadAsStringAsync(ct);
            return new AgentError(AgentErrorCategory.LlmCall,
                $"LLM provider returned {(int)response.StatusCode}: {errorBody}");
        }
        return response;
    }

    private async Task<AgentResult<HttpResponseMessage>> DispatchAnthropicAsync(
        string apiKey,
        string model,
        JsonElement requestBody,
        CancellationToken ct)
    {
        var translated = AnthropicTranslator.TranslateRequest(requestBody, model);
        var client = _httpClientFactory.CreateClient("llm-proxy");
        var req = new HttpRequestMessage(HttpMethod.Post, "https://api.anthropic.com/v1/messages")
        {
            Content = new StringContent(translated, Encoding.UTF8, "application/json"),
        };
        req.Headers.Add("x-api-key", apiKey);
        req.Headers.Add("anthropic-version", "2023-06-01");
        req.Headers.Accept.ParseAdd("text/event-stream");

        var upstream = await client.SendAsync(req, HttpCompletionOption.ResponseHeadersRead, ct);

        if (!upstream.IsSuccessStatusCode)
        {
            var errorBody = await upstream.Content.ReadAsStringAsync(ct);
            return new AgentError(AgentErrorCategory.LlmCall,
                $"Anthropic returned {(int)upstream.StatusCode}: {errorBody}");
        }

        // Wrap Anthropic SSE stream into OpenAI-compatible SSE stream
        var translatedStream = AnthropicTranslator.TranslateStream(
            await upstream.Content.ReadAsStreamAsync(ct));
        var response = new HttpResponseMessage(upstream.StatusCode);
        response.Content = new StreamContent(translatedStream);
        response.Content.Headers.ContentType = new MediaTypeHeaderValue("text/event-stream");
        return response;
    }

    private static string EscapeJson(string s) =>
        s.Replace("\\", "\\\\").Replace("\"", "\\\"");
}
