namespace OffceOs.Infrastructure.Features.Providers;

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
    private readonly ICloudProviderTokenService _cloudProviderTokenService;
    private readonly Func<DateTimeOffset> _utcNow;

    public LlmProviderDispatcher(
        IHttpClientFactory httpFactory,
        ILogger<LlmProviderDispatcher> logger,
        ICloudProviderTokenService? cloudProviderTokenService = null,
        Func<DateTimeOffset>? utcNow = null)
    {
        _httpClientFactory = httpFactory;
        _logger = logger;
        _cloudProviderTokenService = cloudProviderTokenService ?? new CloudProviderTokenService();
        _utcNow = utcNow ?? (() => DateTimeOffset.UtcNow);
    }

    public bool IsSupported(string provider) =>
        ProviderRegistry.Get(provider) is not null ||
        ProviderRegistry.IsCustomProvider(provider);

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
        return await DispatchAsync(
            provider,
            string.IsNullOrWhiteSpace(apiKey)
                ? new ProviderAuthResult(ProviderAuthKind.ApiKey, new Dictionary<string, string>())
                : new ProviderAuthResult(ProviderAuthKind.ApiKey, new Dictionary<string, string> { ["apiKey"] = apiKey }),
            model,
            requestBody,
            ct);
    }

    public async Task<AgentResult<LlmDispatchResponse>> DispatchAsync(
        string provider,
        ProviderAuthResult auth,
        string model,
        JsonElement requestBody,
        CancellationToken ct)
    {
        var definition = ProviderRegistry.Get(provider);
        var isCustomProvider = definition is null &&
            ProviderRegistry.IsCustomProvider(provider);
        var customBaseUrl = auth.Get("baseUrl");
        var isConfiguredCustomProvider = isCustomProvider &&
            !string.IsNullOrWhiteSpace(customBaseUrl);

        if (definition is null && !isConfiguredCustomProvider)
            return new AgentError(AgentErrorCategory.Configuration, $"Unsupported provider: {provider}");
        if (isCustomProvider && string.IsNullOrWhiteSpace(customBaseUrl))
            return new AgentError(AgentErrorCategory.Configuration, "Provider 'custom' requires credentials.baseUrl.");
        if (isConfiguredCustomProvider && model.Equals("auto", StringComparison.OrdinalIgnoreCase))
        {
            return new AgentError(
                AgentErrorCategory.Configuration,
                "Provider 'custom' does not support auto model routing.");
        }

        if (definition is not null &&
            model.Equals("auto", StringComparison.OrdinalIgnoreCase) &&
            ProviderRegistry.GetSmartRouteModel(definition.Slug, SmartRoutingTier.Standard) is null)
        {
            return new AgentError(
                AgentErrorCategory.Configuration,
                $"Provider '{provider}' does not support auto model routing.");
        }

        var resolvedModel = isConfiguredCustomProvider
            ? model
            : model.Equals("auto", StringComparison.OrdinalIgnoreCase)
                ? SmartRouter.Resolve(model, requestBody, definition!.Slug)
                : model;

        _logger.LogInformation("Dispatching LLM request to {Provider} model {Model}", provider, resolvedModel);

        try
        {
            if (definition?.ApiFormat == ApiFormat.Anthropic)
                return await DispatchAnthropicAsync(definition.Slug, auth, resolvedModel, requestBody, ct);

            var baseUrl = isConfiguredCustomProvider
                ? customBaseUrl!
                : definition!.Slug.Equals(ProviderRegistry.AzureFoundryProviderSlug, StringComparison.OrdinalIgnoreCase) && HasFoundryEndpoint(auth)
                    ? $"{FoundryEndpoint(auth).TrimEnd('/')}/openai/v1"
                    : definition!.BaseUrl;
            return await DispatchOpenAiCompatAsync(baseUrl, auth, resolvedModel, requestBody, ct);
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

    public async Task<AgentResult<bool>> CheckModelAccessAsync(
        string provider,
        ProviderAuthResult auth,
        string model,
        CancellationToken ct)
    {
        using var document = JsonDocument.Parse(
            """
            {
              "messages": [
                {
                  "role": "user",
                  "content": "ping"
                }
              ],
              "max_tokens": 1,
              "stream": true
            }
            """);
        var result = await DispatchAsync(provider, auth, model, document.RootElement, ct);
        if (result.IsFailure)
            return result.Error;

        result.Value.Response.Dispose();
        return true;
    }

    private async Task<AgentResult<LlmDispatchResponse>> DispatchOpenAiCompatAsync(
        string baseUrl,
        ProviderAuthResult auth,
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

        var response = await SendOpenAiCompatRequestAsync(baseUrl, auth, dict, ct);
        if (!response.IsSuccessStatusCode)
        {
            var errorBody = await response.Content.ReadAsStringAsync(ct);
            if (dict.Remove("stream_options") && ShouldRetryWithoutUsageOption(response.StatusCode, errorBody))
            {
                response.Dispose();
                response = await SendOpenAiCompatRequestAsync(baseUrl, auth, dict, ct);
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
        ProviderAuthResult auth,
        Dictionary<string, JsonElement> body,
        CancellationToken ct)
    {
        var json = JsonSerializer.Serialize(body);
        var client = _httpClientFactory.CreateClient("llm-proxy");
        var req = new HttpRequestMessage(HttpMethod.Post, $"{baseUrl.TrimEnd('/')}/chat/completions")
        {
            Content = new StringContent(json, Encoding.UTF8, "application/json"),
        };
        await ApplyOpenAiCompatAuthAsync(req, auth, ct);
        req.Headers.Accept.ParseAdd("text/event-stream");

        return await client.SendAsync(req, HttpCompletionOption.ResponseHeadersRead, ct);
    }

    private async Task ApplyOpenAiCompatAuthAsync(HttpRequestMessage request, ProviderAuthResult auth, CancellationToken ct)
    {
        switch (auth.Kind)
        {
            case ProviderAuthKind.ApiKey:
                if (!string.IsNullOrWhiteSpace(auth.Get("apiKey")))
                    request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", auth.Get("apiKey"));
                break;
            case ProviderAuthKind.Gateway:
                break;
            case ProviderAuthKind.AzureApiKey:
                request.Headers.Add("api-key", Required(auth, "apiKey"));
                break;
            case ProviderAuthKind.AzureDefaultCredential:
            case ProviderAuthKind.AzureEntraClientSecret:
            case ProviderAuthKind.AzureManagedIdentity:
                request.Headers.Authorization = new AuthenticationHeaderValue(
                    "Bearer",
                    await _cloudProviderTokenService.GetAzureAccessTokenAsync(auth, ct));
                break;
            case ProviderAuthKind.CodexChatGptOAuth:
                request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", Required(auth, "accessToken"));
                if (!string.IsNullOrWhiteSpace(auth.Get("accountId")))
                    request.Headers.Add("ChatGPT-Account-Id", auth.Get("accountId"));
                break;
            default:
                throw new InvalidOperationException($"Authentication kind '{auth.Kind.ToStorageString()}' is not supported for OpenAI-compatible dispatch.");
        }
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
        string provider,
        ProviderAuthResult auth,
        string model,
        JsonElement requestBody,
        CancellationToken ct)
    {
        var translated = AnthropicTranslator.TranslateRequest(requestBody, model);
        var client = _httpClientFactory.CreateClient("llm-proxy");
        var req = new HttpRequestMessage(HttpMethod.Post, AnthropicEndpoint(provider, auth, model))
        {
            Content = new StringContent(translated, Encoding.UTF8, "application/json"),
        };
        await ApplyAnthropicAuthAsync(req, provider, auth, ct);
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

    private static string AnthropicEndpoint(string provider, ProviderAuthResult auth, string model) => provider switch
    {
        ProviderRegistry.AwsBedrockProviderSlug =>
            $"{BedrockBaseUrl(auth)}/model/{Uri.EscapeDataString(model)}/invoke-with-response-stream",
        ProviderRegistry.GoogleVertexProviderSlug =>
            string.IsNullOrWhiteSpace(auth.Get("projectId")) || string.IsNullOrWhiteSpace(auth.Get("location"))
                ? $"{AnthropicBaseUrl(auth, "https://aiplatform.googleapis.com")}/v1/messages"
                : $"{AnthropicBaseUrl(auth, $"https://{Required(auth, "location")}-aiplatform.googleapis.com")}/v1/projects/{Required(auth, "projectId")}/locations/{Required(auth, "location")}/publishers/anthropic/models/{Uri.EscapeDataString(model)}:streamRawPredict",
        ProviderRegistry.AzureFoundryProviderSlug =>
            $"{FoundryEndpoint(auth).TrimEnd('/')}/openai/v1/chat/completions",
        _ => "https://api.anthropic.com/v1/messages",
    };

    private async Task ApplyAnthropicAuthAsync(HttpRequestMessage request, string provider, ProviderAuthResult auth, CancellationToken ct)
    {
        switch (provider, auth.Kind)
        {
            case (_, ProviderAuthKind.ApiKey):
                request.Headers.Add("x-api-key", Required(auth, "apiKey"));
                break;
            case (_, ProviderAuthKind.Gateway):
                break;
            case (ProviderRegistry.AwsBedrockProviderSlug, ProviderAuthKind.AwsEnvironment):
            case (ProviderRegistry.AwsBedrockProviderSlug, ProviderAuthKind.AwsProfile):
            case (ProviderRegistry.AwsBedrockProviderSlug, ProviderAuthKind.AwsAccessKey):
            case (ProviderRegistry.AwsBedrockProviderSlug, ProviderAuthKind.AwsIam):
                ApplyAwsSigV4(request, await _cloudProviderTokenService.GetAwsCredentialsAsync(auth, ct));
                break;
            case (ProviderRegistry.AwsBedrockProviderSlug, ProviderAuthKind.AwsBedrockApiKey):
                request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", Required(auth, "apiKey"));
                break;
            case (ProviderRegistry.GoogleVertexProviderSlug, ProviderAuthKind.GoogleServiceAccountFile):
            case (ProviderRegistry.GoogleVertexProviderSlug, ProviderAuthKind.GoogleServiceAccount):
            case (ProviderRegistry.GoogleVertexProviderSlug, ProviderAuthKind.GoogleApplicationDefault):
                request.Headers.Authorization = new AuthenticationHeaderValue(
                    "Bearer",
                    await _cloudProviderTokenService.GetGoogleAccessTokenAsync(auth, ct));
                break;
            case (ProviderRegistry.AzureFoundryProviderSlug, ProviderAuthKind.AzureDefaultCredential):
            case (ProviderRegistry.AzureFoundryProviderSlug, ProviderAuthKind.AzureEntraClientSecret):
            case (ProviderRegistry.AzureFoundryProviderSlug, ProviderAuthKind.AzureManagedIdentity):
                request.Headers.Authorization = new AuthenticationHeaderValue(
                    "Bearer",
                    await _cloudProviderTokenService.GetAzureAccessTokenAsync(auth, ct));
                break;
            case (ProviderRegistry.AzureFoundryProviderSlug, ProviderAuthKind.AzureApiKey):
                request.Headers.Add("api-key", Required(auth, "apiKey"));
                break;
            default:
                throw new InvalidOperationException($"Authentication kind '{auth.Kind.ToStorageString()}' is not supported for provider '{provider}'.");
        }
    }

    private void ApplyAwsSigV4(HttpRequestMessage request, ProviderAuthResult auth)
    {
        var body = request.Content?.ReadAsStringAsync().GetAwaiter().GetResult() ?? string.Empty;
        var now = _utcNow();
        var amzDate = now.UtcDateTime.ToString("yyyyMMdd'T'HHmmss'Z'");
        var dateStamp = now.UtcDateTime.ToString("yyyyMMdd");
        var region = Required(auth, "awsRegion");
        var host = request.RequestUri?.Host ?? throw new InvalidOperationException("AWS request URI host is required.");
        var payloadHash = ToHex(SHA256.HashData(Encoding.UTF8.GetBytes(body)));

        request.Headers.Host = host;
        request.Headers.TryAddWithoutValidation("x-amz-date", amzDate);
        request.Headers.TryAddWithoutValidation("x-amz-content-sha256", payloadHash);
        if (!string.IsNullOrWhiteSpace(auth.Get("awsSessionToken")))
            request.Headers.TryAddWithoutValidation("x-amz-security-token", auth.Get("awsSessionToken"));

        var signedHeaders = "host;x-amz-content-sha256;x-amz-date" +
            (!string.IsNullOrWhiteSpace(auth.Get("awsSessionToken")) ? ";x-amz-security-token" : string.Empty);
        var canonicalHeaders =
            $"host:{host}\n" +
            $"x-amz-content-sha256:{payloadHash}\n" +
            $"x-amz-date:{amzDate}\n" +
            (!string.IsNullOrWhiteSpace(auth.Get("awsSessionToken")) ? $"x-amz-security-token:{auth.Get("awsSessionToken")}\n" : string.Empty);
        var canonicalRequest =
            $"{request.Method.Method}\n{request.RequestUri!.AbsolutePath}\n{request.RequestUri.Query.TrimStart('?')}\n{canonicalHeaders}\n{signedHeaders}\n{payloadHash}";
        var credentialScope = $"{dateStamp}/{region}/bedrock/aws4_request";
        var stringToSign =
            $"AWS4-HMAC-SHA256\n{amzDate}\n{credentialScope}\n{ToHex(SHA256.HashData(Encoding.UTF8.GetBytes(canonicalRequest)))}";
        var signingKey = GetAwsSignatureKey(Required(auth, "awsSecretAccessKey"), dateStamp, region, "bedrock");
        var signature = ToHex(HmacSha256(signingKey, stringToSign));
        request.Headers.TryAddWithoutValidation(
            "Authorization",
            $"AWS4-HMAC-SHA256 Credential={Required(auth, "awsAccessKeyId")}/{credentialScope}, SignedHeaders={signedHeaders}, Signature={signature}");
    }

    private static byte[] GetAwsSignatureKey(string secretKey, string dateStamp, string regionName, string serviceName)
    {
        var kDate = HmacSha256(Encoding.UTF8.GetBytes($"AWS4{secretKey}"), dateStamp);
        var kRegion = HmacSha256(kDate, regionName);
        var kService = HmacSha256(kRegion, serviceName);
        return HmacSha256(kService, "aws4_request");
    }

    private static byte[] HmacSha256(byte[] key, string data)
    {
        using var hmac = new HMACSHA256(key);
        return hmac.ComputeHash(Encoding.UTF8.GetBytes(data));
    }

    private static string ToHex(byte[] bytes) =>
        Convert.ToHexString(bytes).ToLowerInvariant();

    private static string AnthropicBaseUrl(ProviderAuthResult auth, string fallback) =>
        (auth.Get("baseUrl") ?? fallback).TrimEnd('/');

    private static string BedrockBaseUrl(ProviderAuthResult auth) =>
        !string.IsNullOrWhiteSpace(auth.Get("baseUrl"))
            ? auth.Get("baseUrl")!.TrimEnd('/')
            : $"https://bedrock-runtime.{Required(auth, "awsRegion")}.amazonaws.com";

    private static string FoundryEndpoint(ProviderAuthResult auth)
    {
        if (!string.IsNullOrWhiteSpace(auth.Get("baseUrl")))
            return auth.Get("baseUrl")!;

        if (!string.IsNullOrWhiteSpace(auth.Get("endpoint")))
            return auth.Get("endpoint")!;

        if (!string.IsNullOrWhiteSpace(auth.Get("resource")))
            return $"https://{auth.Get("resource")}.services.ai.azure.com/anthropic";

        throw new InvalidOperationException("Foundry resource or base URL is required.");
    }

    private static bool HasFoundryEndpoint(ProviderAuthResult auth) =>
        !string.IsNullOrWhiteSpace(auth.Get("baseUrl")) ||
        !string.IsNullOrWhiteSpace(auth.Get("endpoint")) ||
        !string.IsNullOrWhiteSpace(auth.Get("resource"));

    private static string Required(ProviderAuthResult auth, string key) =>
        auth.Get(key) ?? throw new InvalidOperationException($"Provider credential '{key}' is required.");
}

public sealed record LlmDispatchResponse(HttpResponseMessage Response, string Model);
