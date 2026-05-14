namespace OffceOs.Infrastructure.Features.Agents.Adapters;

internal sealed class AutoBrowserRuntimeClient : IBrowserRuntimeClient
{
    private readonly HttpClient _httpClient;
    private readonly BrowserRuntimeConfig _browserRuntimeConfig;
    private readonly ILogger<AutoBrowserRuntimeClient> _logger;
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    public AutoBrowserRuntimeClient(HttpClient http, BrowserRuntimeConfig config, ILogger<AutoBrowserRuntimeClient> logger)
    {
        _httpClient = http;
        _browserRuntimeConfig = config;
        _logger = logger;

        _httpClient.BaseAddress = new Uri(_browserRuntimeConfig.BaseUrl.TrimEnd('/') + "/");
        _httpClient.Timeout = TimeSpan.FromSeconds(Math.Max(1, _browserRuntimeConfig.TimeoutSeconds));
        if (!string.IsNullOrWhiteSpace(_browserRuntimeConfig.BearerToken))
            _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", _browserRuntimeConfig.BearerToken);
    }

    public async Task<bool> IsAvailableAsync(CancellationToken ct = default)
    {
        if (!_browserRuntimeConfig.Enabled) return false;
        try
        {
            using var response = await _httpClient.GetAsync("healthz", ct);
            return response.IsSuccessStatusCode;
        }
        catch (Exception ex)
        {
            _logger.LogDebug(ex, "Browser runtime health check failed");
            return false;
        }
    }

    public async Task<BrowserSessionState?> GetSessionAsync(Guid agentId, string runtimeSessionId, CancellationToken ct = default)
    {
        try
        {
            using var response = await _httpClient.GetAsync($"sessions/{Uri.EscapeDataString(runtimeSessionId)}", ct);
            if (response.StatusCode == HttpStatusCode.NotFound) return null;
            response.EnsureSuccessStatusCode();
            using var doc = await JsonDocument.ParseAsync(await response.Content.ReadAsStreamAsync(ct), cancellationToken: ct);
            return MapSession(agentId, doc.RootElement);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to get browser session {RuntimeSessionId}", runtimeSessionId);
            return null;
        }
    }

    public async Task<BrowserSessionState> CreateSessionAsync(Guid agentId, string name, string? authProfile, CancellationToken ct = default)
    {
        var payload = new Dictionary<string, object?>
        {
            ["name"] = name,
        };
        if (!string.IsNullOrWhiteSpace(authProfile))
            payload["auth_profile"] = authProfile;

        using var response = await _httpClient.PostAsJsonAsync("sessions", payload, JsonOptions, ct);
        if (response.StatusCode == HttpStatusCode.NotFound && payload.Remove("auth_profile"))
        {
            using var retry = await _httpClient.PostAsJsonAsync("sessions", payload, JsonOptions, ct);
            retry.EnsureSuccessStatusCode();
            using var retryDoc = await JsonDocument.ParseAsync(await retry.Content.ReadAsStreamAsync(ct), cancellationToken: ct);
            return MapSession(agentId, retryDoc.RootElement);
        }
        response.EnsureSuccessStatusCode();
        using var doc = await JsonDocument.ParseAsync(await response.Content.ReadAsStreamAsync(ct), cancellationToken: ct);
        return MapSession(agentId, doc.RootElement);
    }

    public async Task CloseSessionAsync(string runtimeSessionId, CancellationToken ct = default)
    {
        using var response = await _httpClient.DeleteAsync($"sessions/{Uri.EscapeDataString(runtimeSessionId)}", ct);
        if (response.StatusCode == HttpStatusCode.NotFound) return;
        response.EnsureSuccessStatusCode();
    }

    public async Task<IReadOnlyList<BrowserToolDescriptor>> ListToolsAsync(CancellationToken ct = default)
    {
        using var response = await _httpClient.GetAsync("mcp/tools", ct);
        response.EnsureSuccessStatusCode();
        using var doc = await JsonDocument.ParseAsync(await response.Content.ReadAsStreamAsync(ct), cancellationToken: ct);
        var tools = new List<BrowserToolDescriptor>();
        foreach (var item in doc.RootElement.EnumerateArray())
        {
            var name = item.GetProperty("name").GetString() ?? "";
            if (string.IsNullOrWhiteSpace(name)) continue;
            var description = item.TryGetProperty("description", out var d) ? d.GetString() ?? name : name;
            var schema = item.TryGetProperty("inputSchema", out var s)
                ? JsonSerializer.Deserialize<JsonElement>(s.GetRawText())
                : JsonSerializer.SerializeToElement(new { type = "object", properties = new { } });
            tools.Add(new BrowserToolDescriptor(name, description, schema));
        }
        return tools;
    }

    public async Task<BrowserToolCallResult> CallToolAsync(string name, Dictionary<string, object?> arguments, CancellationToken ct = default)
    {
        using var response = await _httpClient.PostAsJsonAsync("mcp/tools/call", new { name, arguments }, JsonOptions, ct);
        response.EnsureSuccessStatusCode();
        using var doc = await JsonDocument.ParseAsync(await response.Content.ReadAsStreamAsync(ct), cancellationToken: ct);
        var root = doc.RootElement;
        var isError = root.TryGetProperty("isError", out var e) && e.ValueKind == JsonValueKind.True;
        var output = new StringBuilder();
        if (root.TryGetProperty("content", out var content) && content.ValueKind == JsonValueKind.Array)
        {
            foreach (var item in content.EnumerateArray())
            {
                if (item.TryGetProperty("text", out var text) && text.ValueKind == JsonValueKind.String)
                    output.AppendLine(text.GetString());
            }
        }
        return new BrowserToolCallResult(isError, output.ToString().Trim());
    }

    private BrowserSessionState MapSession(Guid agentId, JsonElement item)
    {
        var runtimeSessionId = ReadString(item, "id") ?? ReadString(item, "session_id") ?? "";
        var remoteAccess = item.TryGetProperty("remote_access", out var ra) ? ra : default;
        var takeoverUrl = ReadString(item, "takeover_url")
            ?? (remoteAccess.ValueKind == JsonValueKind.Object ? ReadString(remoteAccess, "takeover_url") : null);

        if (!string.IsNullOrWhiteSpace(takeoverUrl) && !string.IsNullOrWhiteSpace(_browserRuntimeConfig.PublicViewBaseUrl))
            takeoverUrl = RewriteViewBase(takeoverUrl, _browserRuntimeConfig.PublicViewBaseUrl);

        return new BrowserSessionState(
            AgentId: agentId,
            RuntimeSessionId: runtimeSessionId,
            Status: ReadString(item, "status") ?? "unknown",
            Name: ReadString(item, "name"),
            CurrentUrl: ReadString(item, "url") ?? ReadString(item, "current_url"),
            Title: ReadString(item, "title"),
            TakeoverUrl: takeoverUrl,
            CreatedAt: ReadDate(item, "created_at"),
            LastAccessedAt: DateTime.UtcNow);
    }

    private static string RewriteViewBase(string url, string publicBase)
    {
        if (!Uri.TryCreate(url, UriKind.Absolute, out var source)) return url;
        if (!Uri.TryCreate(publicBase.TrimEnd('/'), UriKind.Absolute, out var target)) return url;
        var builder = new UriBuilder(source)
        {
            Scheme = target.Scheme,
            Host = target.Host,
            Port = target.IsDefaultPort ? -1 : target.Port,
        };
        return builder.Uri.ToString();
    }

    private static string? ReadString(JsonElement item, string name)
        => item.ValueKind == JsonValueKind.Object
           && item.TryGetProperty(name, out var value)
           && value.ValueKind == JsonValueKind.String
            ? value.GetString()
            : null;

    private static DateTime? ReadDate(JsonElement item, string name)
        => DateTime.TryParse(ReadString(item, name), out var date) ? date : null;
}
