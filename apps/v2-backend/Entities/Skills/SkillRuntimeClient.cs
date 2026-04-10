using System.Text.Json;

namespace EnterpriseAgentOs.Api.Entities.Skills;

/// <summary>
/// HTTP client that dispatches skill execution and manifest fetching
/// to the external skill-runtime service.
/// </summary>
public sealed class SkillRuntimeClient
{
    private readonly HttpClient _http;
    private readonly string _baseUrl;

    // Cached manifests (refreshed periodically)
    private IReadOnlyList<RuntimeManifest>? _cachedManifests;
    private DateTime _lastManifestFetch = DateTime.MinValue;
    private static readonly TimeSpan ManifestCacheTtl = TimeSpan.FromMinutes(5);

    public SkillRuntimeClient(HttpClient http, SkillRuntimeConfig config)
    {
        _http = http;
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
        CancellationToken ct = default)
    {
        var body = JsonSerializer.Serialize(new
        {
            skill,
            action,
            @params = parameters,
            credentials,
        });

        using var req = new HttpRequestMessage(HttpMethod.Post, $"{_baseUrl}/execute");
        req.Content = new StringContent(body, System.Text.Encoding.UTF8, "application/json");
        using var resp = await _http.SendAsync(req, ct);
        var text = await resp.Content.ReadAsStringAsync(ct);

        try
        {
            var result = JsonSerializer.Deserialize<SkillExecutionResult>(text, JsonOptions);
            return result ?? new SkillExecutionResult { Success = false, Error = "Empty response from runtime" };
        }
        catch (JsonException)
        {
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
            // Return cached or empty
            return _cachedManifests ?? Array.Empty<RuntimeManifest>();
        }
        var text = await resp.Content.ReadAsStringAsync(ct);
        var manifests = JsonSerializer.Deserialize<List<RuntimeManifest>>(text, JsonOptions)
            ?? new List<RuntimeManifest>();
        _cachedManifests = manifests;
        _lastManifestFetch = DateTime.UtcNow;
        return manifests;
    }

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        PropertyNameCaseInsensitive = true,
    };

    private static string Trim(string s, int max) => s.Length <= max ? s : s[..max];
}

/// <summary>Result from POST /execute on the skill runtime.</summary>
public sealed class SkillExecutionResult
{
    public bool Success { get; set; }
    public JsonElement? Result { get; set; }
    public string? Error { get; set; }
}

/// <summary>Manifest returned by GET /manifests from the skill runtime.</summary>
public sealed class RuntimeManifest
{
    public string Name { get; set; } = "";
    public string Description { get; set; } = "";
    public string? Doc { get; set; }
    public Dictionary<string, RuntimeActionManifest> Actions { get; set; } = new();
    public Dictionary<string, RuntimeCredentialManifest> Credentials { get; set; } = new();
}

public sealed class RuntimeActionManifest
{
    public string Description { get; set; } = "";
    public JsonElement? Params { get; set; }
    public JsonElement? Returns { get; set; }
}

public sealed class RuntimeCredentialManifest
{
    public string Description { get; set; } = "";
}
