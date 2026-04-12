using System.Text.Json;

namespace EnterpriseAgentOs.Api.Entities.Skills;

/// <summary>
/// HTTP client that dispatches skill execution and manifest fetching
/// to the external skill-runtime service.
/// </summary>
public sealed class SkillRuntimeClient
{
    private readonly HttpClient _http;
    private readonly ILogger<SkillRuntimeClient> _logger;
    private readonly string _baseUrl;

    // Cached manifests (refreshed periodically)
    private IReadOnlyList<RuntimeManifest>? _cachedManifests;
    private DateTime _lastManifestFetch = DateTime.MinValue;
    private static readonly TimeSpan ManifestCacheTtl = TimeSpan.FromSeconds(30);

    public SkillRuntimeClient(HttpClient http, SkillRuntimeConfig config, ILogger<SkillRuntimeClient> logger)
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
    /// Fetch manifests from the runtime (cached for 5 minutes).
    /// </summary>
    public async Task<IReadOnlyList<RuntimeManifest>> GetManifestsAsync(CancellationToken ct = default)
    {
        if (_cachedManifests is not null && DateTime.UtcNow - _lastManifestFetch < ManifestCacheTtl)
        {
            return _cachedManifests;
        }

        using var resp = await _http.GetAsync($"{_baseUrl}/manifests", ct);
        if (!resp.IsSuccessStatusCode)
        {
            _logger.LogWarning("Failed to fetch skill manifests from runtime (HTTP {StatusCode})", (int)resp.StatusCode);
            return _cachedManifests ?? Array.Empty<RuntimeManifest>();
        }
        var text = await resp.Content.ReadAsStringAsync(ct);
        var manifests = JsonSerializer.Deserialize<List<RuntimeManifest>>(text, JsonOptions)
            ?? new List<RuntimeManifest>();
        _cachedManifests = manifests;
        _lastManifestFetch = DateTime.UtcNow;
        _logger.LogDebug("Refreshed {Count} skill manifests from runtime", manifests.Count);
        return manifests;
    }

    /// <summary>
    /// Send skill source files to the runtime for building and hot-loading.
    /// </summary>
    public async Task<JsonElement> BuildAsync(string name, object files, CancellationToken ct = default)
    {
        var body = JsonSerializer.Serialize(new { name, files });
        using var req = new HttpRequestMessage(HttpMethod.Post, $"{_baseUrl}/build");
        req.Content = new StringContent(body, System.Text.Encoding.UTF8, "application/json");
        using var resp = await _http.SendAsync(req, ct);

        if (!resp.IsSuccessStatusCode)
        {
            var errorText = await resp.Content.ReadAsStringAsync(ct);
            _logger.LogError("Skill build failed for {SkillName} (HTTP {StatusCode}): {Error}",
                name, (int)resp.StatusCode, Trim(errorText, 200));
            throw new HttpRequestException($"Build failed (HTTP {(int)resp.StatusCode}): {Trim(errorText, 500)}");
        }

        var text = await resp.Content.ReadAsStringAsync(ct);
        // Invalidate manifest cache so new skill appears immediately
        _cachedManifests = null;
        _logger.LogInformation("Skill {SkillName} built successfully", name);
        return JsonSerializer.Deserialize<JsonElement>(text);
    }

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        PropertyNameCaseInsensitive = true,
    };

    private static string Trim(string s, int max) => s.Length <= max ? s : s[..max];
}
