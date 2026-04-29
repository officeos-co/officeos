namespace EnterpriseAgentOs.Infrastructure.Features.Analytics;

/// <summary>
/// Forwards dashboard/server events to PostHog. Fire-and-forget: failures are
/// logged at warn and swallowed — analytics must never break a user request.
///
/// NOTE: No batching. Every capture is a single HTTP POST to /capture/. If
/// event volume grows we should switch to /batch/ with an in-memory queue +
/// periodic flush (future optimization).
/// </summary>
internal sealed class PostHogService : IPostHogService
{
    private readonly HttpClient _http;
    private readonly PostHogConfig _postHogConfig;
    private readonly ILogger<PostHogService> _logger;

    public PostHogService(HttpClient http, PostHogConfig config, ILogger<PostHogService> logger)
    {
        _http = http;
        _postHogConfig = config;
        _logger = logger;
    }

    public Task CaptureAsync(
        string distinctId,
        string eventName,
        IReadOnlyDictionary<string, object?>? properties,
        CancellationToken ct = default)
        => PostAsync("/capture/", BuildCapturePayload(distinctId, eventName, properties), ct);

    public Task IdentifyAsync(
        string distinctId,
        IReadOnlyDictionary<string, object?>? traits,
        CancellationToken ct = default)
    {
        // PostHog convention: $identify is an event with $set on properties.
        var props = new Dictionary<string, object?>();
        if (traits is not null)
        {
            props["$set"] = traits;
        }
        return PostAsync("/capture/", BuildCapturePayload(distinctId, "$identify", props), ct);
    }

    private Dictionary<string, object?> BuildCapturePayload(
        string distinctId,
        string eventName,
        IReadOnlyDictionary<string, object?>? properties)
    {
        return new Dictionary<string, object?>
        {
            ["api_key"] = _postHogConfig.ApiKey,
            ["event"] = eventName,
            ["distinct_id"] = distinctId,
            ["properties"] = properties ?? new Dictionary<string, object?>(),
            ["timestamp"] = DateTime.UtcNow.ToString("o"),
        };
    }

    private async Task PostAsync(string path, object payload, CancellationToken ct)
    {
        if (!_postHogConfig.Enabled || string.IsNullOrWhiteSpace(_postHogConfig.ApiKey))
        {
            return;
        }

        try
        {
            var url = _postHogConfig.Host.TrimEnd('/') + path;
            var json = JsonSerializer.Serialize(payload);
            using var content = new StringContent(json, Encoding.UTF8, "application/json");
            using var response = await _http.PostAsync(url, content, ct);
            if (!response.IsSuccessStatusCode)
            {
                _logger.LogWarning(
                    "PostHog capture failed: status {Status} for path {Path}",
                    (int)response.StatusCode, path);
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "PostHog capture threw; swallowing so analytics never breaks user requests");
        }
    }
}
