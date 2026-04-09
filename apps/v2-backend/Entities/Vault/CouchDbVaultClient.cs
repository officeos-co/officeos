using System.Net;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;

namespace EnterpriseAgentOs.Api.Entities.Vault;

public sealed class CouchDbVaultClient : IVaultClient
{
    private readonly HttpClient _http;
    private readonly ILogger<CouchDbVaultClient> _logger;
    private readonly string _baseUrl;

    public CouchDbVaultClient(HttpClient http, CouchDbConfig config, ILogger<CouchDbVaultClient> logger)
    {
        _http = http;
        _logger = logger;
        _baseUrl = config.Url.TrimEnd('/');

        var token = Convert.ToBase64String(Encoding.ASCII.GetBytes($"{config.User}:{config.Password}"));
        _http.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Basic", token);
    }

    public static string DbName(Guid agentId) =>
        $"eaos-agent-{agentId.ToString("d").ToLowerInvariant()}";

    public async Task CreateAgentVaultAsync(Guid agentId, string agentName, string provider, string? model, CancellationToken ct = default)
    {
        var db = DbName(agentId);

        using (var createReq = new HttpRequestMessage(HttpMethod.Put, $"{_baseUrl}/{db}"))
        using (var createResp = await _http.SendAsync(createReq, ct))
        {
            if (!createResp.IsSuccessStatusCode
                && createResp.StatusCode != HttpStatusCode.PreconditionFailed
                && createResp.StatusCode != HttpStatusCode.Created
                && createResp.StatusCode != HttpStatusCode.Accepted)
            {
                var body = await createResp.Content.ReadAsStringAsync(ct);
                throw new InvalidOperationException(
                    $"CouchDB create db {db} failed: {(int)createResp.StatusCode} {body}");
            }
        }

        var files = VaultPersonalityTemplate.Render(agentId, agentName, provider, model);
        foreach (var (name, content) in files)
        {
            await PutDocumentAsync(db, name, content, ct);
        }

        _logger.LogInformation("Seeded vault {Db} with {Count} files", db, files.Count);
    }

    public async Task DeleteAgentVaultAsync(Guid agentId, CancellationToken ct = default)
    {
        var db = DbName(agentId);
        using var resp = await _http.DeleteAsync($"{_baseUrl}/{db}", ct);
        if (!resp.IsSuccessStatusCode && resp.StatusCode != HttpStatusCode.NotFound)
        {
            var body = await resp.Content.ReadAsStringAsync(ct);
            _logger.LogWarning("CouchDB delete db {Db} failed: {Status} {Body}",
                db, (int)resp.StatusCode, body);
        }
    }

    public async Task<IReadOnlyList<string>> ListFilesAsync(Guid agentId, CancellationToken ct = default)
    {
        var db = DbName(agentId);
        using var resp = await _http.GetAsync($"{_baseUrl}/{db}/_all_docs", ct);
        if (!resp.IsSuccessStatusCode)
        {
            return Array.Empty<string>();
        }
        var body = await resp.Content.ReadAsStringAsync(ct);
        using var doc = JsonDocument.Parse(body);
        if (!doc.RootElement.TryGetProperty("rows", out var rows))
        {
            return Array.Empty<string>();
        }
        var result = new List<string>();
        foreach (var row in rows.EnumerateArray())
        {
            if (row.TryGetProperty("id", out var idEl) && idEl.GetString() is { } id
                && !id.StartsWith("_design/", StringComparison.Ordinal))
            {
                result.Add(id);
            }
        }
        result.Sort(StringComparer.Ordinal);
        return result;
    }

    public async Task<string?> GetFileAsync(Guid agentId, string fileName, CancellationToken ct = default)
    {
        var db = DbName(agentId);
        using var resp = await _http.GetAsync($"{_baseUrl}/{db}/{Uri.EscapeDataString(fileName)}", ct);
        if (resp.StatusCode == HttpStatusCode.NotFound)
        {
            return null;
        }
        if (!resp.IsSuccessStatusCode)
        {
            var err = await resp.Content.ReadAsStringAsync(ct);
            throw new InvalidOperationException(
                $"CouchDB get {db}/{fileName} failed: {(int)resp.StatusCode} {err}");
        }
        var body = await resp.Content.ReadAsStringAsync(ct);
        using var doc = JsonDocument.Parse(body);
        return doc.RootElement.TryGetProperty("content", out var content) ? content.GetString() : null;
    }

    private async Task PutDocumentAsync(string db, string docId, string content, CancellationToken ct)
    {
        string? rev = await GetCurrentRevAsync(db, docId, ct);

        var payload = new Dictionary<string, string> { ["content"] = content };
        if (rev is not null)
        {
            payload["_rev"] = rev;
        }

        var json = JsonSerializer.Serialize(payload);
        using var req = new HttpRequestMessage(HttpMethod.Put, $"{_baseUrl}/{db}/{docId}")
        {
            Content = new StringContent(json, Encoding.UTF8, "application/json"),
        };
        using var resp = await _http.SendAsync(req, ct);
        if (!resp.IsSuccessStatusCode)
        {
            var body = await resp.Content.ReadAsStringAsync(ct);
            throw new InvalidOperationException(
                $"CouchDB put {db}/{docId} failed: {(int)resp.StatusCode} {body}");
        }
    }

    private async Task<string?> GetCurrentRevAsync(string db, string docId, CancellationToken ct)
    {
        using var resp = await _http.GetAsync($"{_baseUrl}/{db}/{docId}", ct);
        if (resp.StatusCode == HttpStatusCode.NotFound)
        {
            return null;
        }
        if (!resp.IsSuccessStatusCode)
        {
            return null;
        }
        var body = await resp.Content.ReadAsStringAsync(ct);
        using var doc = JsonDocument.Parse(body);
        return doc.RootElement.TryGetProperty("_rev", out var rev) ? rev.GetString() : null;
    }
}
