namespace EnterpriseAgentOs.Api.Entities.LlmProxy;

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
            ["ollama"] = "http://localhost:11434/v1",
        };

    private readonly IHttpClientFactory _httpFactory;
    private readonly ILogger<LlmProviderDispatcher> _logger;

    public LlmProviderDispatcher(
        IHttpClientFactory httpFactory,
        ILogger<LlmProviderDispatcher> logger)
    {
        _httpFactory = httpFactory;
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
    public async Task<HttpResponseMessage> DispatchAsync(
        string provider,
        string apiKey,
        string model,
        JsonElement requestBody,
        CancellationToken ct)
    {
        _logger.LogInformation("Dispatching LLM request to {Provider} model {Model}", provider, model);

        if (provider.Equals("anthropic", StringComparison.OrdinalIgnoreCase))
        {
            return await DispatchAnthropicAsync(apiKey, model, requestBody, ct);
        }

        if (!OpenAiCompatBaseUrls.TryGetValue(provider, out var baseUrl))
        {
            _logger.LogError("Unsupported provider {Provider} in dispatcher", provider);
            throw new InvalidOperationException($"Unsupported provider: {provider}");
        }

        return await DispatchOpenAiCompatAsync(baseUrl, apiKey, model, requestBody, ct);
    }

    private async Task<HttpResponseMessage> DispatchOpenAiCompatAsync(
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
        var client = _httpFactory.CreateClient("llm-proxy");
        var req = new HttpRequestMessage(HttpMethod.Post, $"{baseUrl.TrimEnd('/')}/chat/completions")
        {
            Content = new StringContent(json, Encoding.UTF8, "application/json"),
        };
        req.Headers.Authorization = new AuthenticationHeaderValue("Bearer", apiKey);
        req.Headers.Accept.ParseAdd("text/event-stream");

        return await client.SendAsync(req, HttpCompletionOption.ResponseHeadersRead, ct);
    }

    private async Task<HttpResponseMessage> DispatchAnthropicAsync(
        string apiKey,
        string model,
        JsonElement requestBody,
        CancellationToken ct)
    {
        var translated = AnthropicTranslator.TranslateRequest(requestBody, model);
        var client = _httpFactory.CreateClient("llm-proxy");
        var req = new HttpRequestMessage(HttpMethod.Post, "https://api.anthropic.com/v1/messages")
        {
            Content = new StringContent(translated, Encoding.UTF8, "application/json"),
        };
        req.Headers.Add("x-api-key", apiKey);
        req.Headers.Add("anthropic-version", "2023-06-01");
        req.Headers.Accept.ParseAdd("text/event-stream");

        var upstream = await client.SendAsync(req, HttpCompletionOption.ResponseHeadersRead, ct);

        // If upstream failed, return as-is (controller will map status)
        if (!upstream.IsSuccessStatusCode)
        {
            return upstream;
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
