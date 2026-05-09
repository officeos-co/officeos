namespace EnterpriseAgentOs.Infrastructure.Features.Agents;

/// <summary>
/// Routes an OpenAI-compatible chat-completions request to the real upstream
/// provider, injecting the real API key. For OpenAI-compatible providers
/// this is a straight passthrough with key + model swap. For Anthropic,
/// the request/response format is translated on the fly.
/// </summary>
public sealed class LlmProviderDispatcher
{
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly ILogger<LlmProviderDispatcher> _logger;
    private readonly CustomLlmProviderConfig _customLlmProviderConfig;

    public LlmProviderDispatcher(
        IHttpClientFactory httpFactory,
        ILogger<LlmProviderDispatcher> logger,
        CustomLlmProviderConfig? customLlmProviderConfig = null)
    {
        _httpClientFactory = httpFactory;
        _logger = logger;
        _customLlmProviderConfig = customLlmProviderConfig ?? new CustomLlmProviderConfig();
    }

    public bool IsSupported(string provider) =>
        ProviderRegistry.Get(provider) is not null ||
        (ProviderRegistry.IsCustomProvider(provider) && _customLlmProviderConfig.IsConfigured);

    /// <summary>
    /// Dispatch a chat-completions request to the upstream provider.
    /// Returns a streaming <see cref="HttpResponseMessage"/> whose body
    /// is standard OpenAI SSE (<c>data: {...}\n\n</c> lines).
    /// </summary>
    public async Task<AgentResult<LlmDispatchResponse>> DispatchAsync(
        string provider,
        string apiKey,
        string model,
        JsonElement requestBody,
        CancellationToken ct)
    {
        var definition = ProviderRegistry.Get(provider);
        var isConfiguredCustomProvider = definition is null &&
            ProviderRegistry.IsCustomProvider(provider) &&
            _customLlmProviderConfig.IsConfigured;

        if (definition is null && !isConfiguredCustomProvider)
            return new AgentError(AgentErrorCategory.Configuration, $"Unsupported provider: {provider}");

        if (isConfiguredCustomProvider && model.Equals("auto", StringComparison.OrdinalIgnoreCase))
        {
            return new AgentError(
                AgentErrorCategory.Configuration,
                "Provider 'custom' does not support auto model routing.");
        }

        if (definition is not null &&
            model.Equals("auto", StringComparison.OrdinalIgnoreCase) &&
            !definition.Slug.Equals("anthropic", StringComparison.OrdinalIgnoreCase))
        {
            return new AgentError(
                AgentErrorCategory.Configuration,
                $"Provider '{provider}' does not support auto model routing.");
        }

        var resolvedModel = isConfiguredCustomProvider
            ? _customLlmProviderConfig.ModelId.Trim()
            : definition!.Slug.Equals("anthropic", StringComparison.OrdinalIgnoreCase)
            ? SmartRouter.Resolve(model, requestBody, definition.Slug)
            : model;

        _logger.LogInformation("Dispatching LLM request to {Provider} model {Model}", provider, resolvedModel);

        try
        {
            if (definition?.ApiFormat == ApiFormat.Anthropic)
                return await DispatchAnthropicAsync(apiKey, resolvedModel, requestBody, ct);

            var baseUrl = isConfiguredCustomProvider ? _customLlmProviderConfig.BaseUrl : definition!.BaseUrl;
            return await DispatchOpenAiCompatAsync(baseUrl, apiKey, resolvedModel, requestBody, ct);
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

    private async Task<AgentResult<LlmDispatchResponse>> DispatchOpenAiCompatAsync(
        string baseUrl,
        string apiKey,
        string model,
        JsonElement requestBody,
        CancellationToken ct)
    {
        var dict = JsonSerializer.Deserialize<Dictionary<string, JsonElement>>(requestBody.GetRawText())
            ?? new Dictionary<string, JsonElement>();
        dict["model"] = JsonDocument.Parse($"\"{EscapeJson(model)}\"").RootElement.Clone();
        using (var streamOptions = JsonDocument.Parse("""{"include_usage":true}"""))
        {
            dict["stream_options"] = streamOptions.RootElement.Clone();
        }

        var response = await SendOpenAiCompatRequestAsync(baseUrl, apiKey, dict, ct);
        if (!response.IsSuccessStatusCode)
        {
            var errorBody = await response.Content.ReadAsStringAsync(ct);
            if (dict.Remove("stream_options") && ShouldRetryWithoutUsageOption(response.StatusCode, errorBody))
            {
                response.Dispose();
                response = await SendOpenAiCompatRequestAsync(baseUrl, apiKey, dict, ct);
                if (response.IsSuccessStatusCode)
                    return new LlmDispatchResponse(response, model);

                errorBody = await response.Content.ReadAsStringAsync(ct);
            }

            return new AgentError(AgentErrorCategory.LlmCall,
                $"LLM provider returned {(int)response.StatusCode}: {errorBody}");
        }
        return new LlmDispatchResponse(response, model);
    }

    private async Task<HttpResponseMessage> SendOpenAiCompatRequestAsync(
        string baseUrl,
        string apiKey,
        Dictionary<string, JsonElement> body,
        CancellationToken ct)
    {
        var json = JsonSerializer.Serialize(body);
        var client = _httpClientFactory.CreateClient("llm-proxy");
        var req = new HttpRequestMessage(HttpMethod.Post, $"{baseUrl.TrimEnd('/')}/chat/completions")
        {
            Content = new StringContent(json, Encoding.UTF8, "application/json"),
        };
        if (!string.IsNullOrWhiteSpace(apiKey))
            req.Headers.Authorization = new AuthenticationHeaderValue("Bearer", apiKey);
        req.Headers.Accept.ParseAdd("text/event-stream");

        return await client.SendAsync(req, HttpCompletionOption.ResponseHeadersRead, ct);
    }

    private static bool ShouldRetryWithoutUsageOption(HttpStatusCode statusCode, string errorBody)
    {
        if (statusCode is not (HttpStatusCode.BadRequest or HttpStatusCode.UnprocessableEntity))
            return false;

        return errorBody.Contains("stream_options", StringComparison.OrdinalIgnoreCase)
            || errorBody.Contains("include_usage", StringComparison.OrdinalIgnoreCase)
            || errorBody.Contains("unknown parameter", StringComparison.OrdinalIgnoreCase)
            || errorBody.Contains("unsupported parameter", StringComparison.OrdinalIgnoreCase);
    }

    private async Task<AgentResult<LlmDispatchResponse>> DispatchAnthropicAsync(
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

        var translatedStream = AnthropicTranslator.TranslateStream(
            await upstream.Content.ReadAsStreamAsync(ct));
        var response = new HttpResponseMessage(upstream.StatusCode);
        response.Content = new StreamContent(translatedStream);
        response.Content.Headers.ContentType = new MediaTypeHeaderValue("text/event-stream");
        return new LlmDispatchResponse(response, model);
    }

    private static string EscapeJson(string s) =>
        s.Replace("\\", "\\\\").Replace("\"", "\\\"");
}

public sealed record LlmDispatchResponse(HttpResponseMessage Response, string Model);
