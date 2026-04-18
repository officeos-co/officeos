namespace EnterpriseAgentOs.Infrastructure.Adapters.SkillRuntime;

/// <summary>
/// HTTP client that dispatches skill execution and manifest fetching
/// to the external skill-runtime service.
/// </summary>
public sealed class SkillRuntimeClient
{
    private readonly HttpClient _http;
    private readonly ILogger<SkillRuntimeClient> _logger;
    private readonly string _baseUrl;

    public SkillRuntimeClient(HttpClient http, EnterpriseAgentOs.Infrastructure.Configuration.SkillRuntimeConfig config, ILogger<SkillRuntimeClient> logger)
    {
        _http = http;
        _logger = logger;
        _baseUrl = config.Url.TrimEnd('/');
    }

    /// <summary>
    /// Execute a skill action via the runtime.
    /// </summary>
    public async Task<SkillExecutionResult> ExecuteAsync(
        string skill,
        string action,
        object parameters,
        IReadOnlyDictionary<string, string> credentials,
        SessionContext? sessionContext = null,
        CancellationToken ct = default)
    {
        var payload = new Dictionary<string, object?>
        {
            ["skill"] = skill,
            ["action"] = action,
            ["params"] = parameters,
            ["credentials"] = credentials,
        };

        if (sessionContext is not null)
        {
            var sc = new Dictionary<string, object?>();
            if (sessionContext.SessionId is not null)
                sc["sessionId"] = sessionContext.SessionId;
            if (sessionContext.CookiesJson is not null)
                sc["cookies"] = JsonSerializer.Deserialize<JsonElement>(sessionContext.CookiesJson);
            payload["sessionContext"] = sc;
        }

        var body = JsonSerializer.Serialize(payload);

        _logger.LogInformation("Executing skill {Skill}.{Action} via runtime", skill, action);

        using var req = new HttpRequestMessage(HttpMethod.Post, $"{_baseUrl}/execute");
        req.Content = new StringContent(body, System.Text.Encoding.UTF8, "application/json");
        using var resp = await _http.SendAsync(req, ct);
        var text = await resp.Content.ReadAsStringAsync(ct);

        try
        {
            var result = JsonSerializer.Deserialize<SkillExecutionResult>(text, JsonOptions);
            if (result is null || !result.Success)
            {
                _logger.LogWarning("Skill {Skill}.{Action} execution failed: {Error}",
                    skill, action, result?.Error ?? "empty response");
            }
            else
            {
                _logger.LogInformation("Skill {Skill}.{Action} executed successfully", skill, action);
            }
            return result ?? new SkillExecutionResult { Success = false, Error = "Empty response from runtime" };
        }
        catch (JsonException)
        {
            _logger.LogError("Skill runtime returned non-JSON for {Skill}.{Action} (HTTP {StatusCode})",
                skill, action, (int)resp.StatusCode);
            return new SkillExecutionResult
            {
                Success = false,
                Error = $"Runtime returned non-JSON (HTTP {(int)resp.StatusCode}): {Trim(text, 500)}"
            };
        }
    }

    /// <summary>
    /// Fetch all skill manifests from the runtime.
    /// </summary>
    public async Task<IReadOnlyList<RuntimeManifest>> GetManifestsAsync(CancellationToken ct = default)
    {
        try
        {
            var text = await _http.GetStringAsync($"{_baseUrl}/manifests", ct);
            return JsonSerializer.Deserialize<List<RuntimeManifest>>(text, JsonOptions) ?? [];
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to fetch manifests from skill-runtime");
            return [];
        }
    }

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        PropertyNameCaseInsensitive = true,
    };

    private static string Trim(string s, int max) => s.Length <= max ? s : s[..max];
}
