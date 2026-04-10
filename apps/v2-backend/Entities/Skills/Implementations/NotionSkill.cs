using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;

namespace EnterpriseAgentOs.Api.Entities.Skills.Implementations;

public sealed record NotionSearchRequest(string Query, int PageSize = 10);
public sealed record NotionReadPageRequest(string PageId);

public sealed class NotionSkill
{
    private const string NotionApi = "https://api.notion.com/v1";
    private const string NotionVersion = "2022-06-28";

    private readonly HttpClient _http;

    public NotionSkill(HttpClient http)
    {
        _http = http;
    }

    public async Task<object> SearchAsync(
        NotionSearchRequest req,
        IReadOnlyDictionary<string, string> creds,
        CancellationToken ct = default)
    {
        var apiKey = GetRequired(creds, "api_key");
        var body = JsonSerializer.Serialize(new { query = req.Query, page_size = req.PageSize });
        var json = await CallAsync(HttpMethod.Post, "/search", apiKey, body, ct);

        var results = new List<object>();
        if (json.TryGetProperty("results", out var arr) && arr.ValueKind == JsonValueKind.Array)
        {
            foreach (var item in arr.EnumerateArray())
            {
                var title = "";
                if (item.TryGetProperty("properties", out var props) && props.ValueKind == JsonValueKind.Object)
                {
                    foreach (var prop in props.EnumerateObject())
                    {
                        if (prop.Value.TryGetProperty("type", out var t) && t.GetString() == "title"
                            && prop.Value.TryGetProperty("title", out var frags)
                            && frags.ValueKind == JsonValueKind.Array)
                        {
                            var sb = new StringBuilder();
                            foreach (var f in frags.EnumerateArray())
                            {
                                if (f.TryGetProperty("plain_text", out var pt))
                                {
                                    sb.Append(pt.GetString());
                                }
                            }
                            title = sb.ToString();
                            break;
                        }
                    }
                }
                results.Add(new
                {
                    id = item.TryGetProperty("id", out var id) ? id.GetString() : null,
                    title = string.IsNullOrEmpty(title) ? "(untitled)" : title,
                    url = item.TryGetProperty("url", out var url) ? url.GetString() : null,
                    @object = item.TryGetProperty("object", out var obj) ? obj.GetString() : null,
                });
            }
        }
        return new { results };
    }

    public async Task<object> ReadPageAsync(
        NotionReadPageRequest req,
        IReadOnlyDictionary<string, string> creds,
        CancellationToken ct = default)
    {
        var apiKey = GetRequired(creds, "api_key");
        var json = await CallAsync(
            HttpMethod.Get,
            $"/blocks/{Uri.EscapeDataString(req.PageId)}/children?page_size=100",
            apiKey,
            body: null,
            ct);

        var lines = new List<string>();
        if (json.TryGetProperty("results", out var arr) && arr.ValueKind == JsonValueKind.Array)
        {
            foreach (var block in arr.EnumerateArray())
            {
                if (!block.TryGetProperty("type", out var btype)) continue;
                var btypeName = btype.GetString();
                if (btypeName is null) continue;
                if (!block.TryGetProperty(btypeName, out var payload)) continue;
                if (!payload.TryGetProperty("rich_text", out var rich) || rich.ValueKind != JsonValueKind.Array) continue;
                var sb = new StringBuilder();
                foreach (var rt in rich.EnumerateArray())
                {
                    if (rt.TryGetProperty("plain_text", out var pt))
                    {
                        sb.Append(pt.GetString());
                    }
                }
                var text = sb.ToString();
                if (!string.IsNullOrEmpty(text)) lines.Add(text);
            }
        }
        return new { page_id = req.PageId, text = string.Join("\n", lines) };
    }

    private async Task<JsonElement> CallAsync(
        HttpMethod method,
        string path,
        string apiKey,
        string? body,
        CancellationToken ct)
    {
        using var req = new HttpRequestMessage(method, $"{NotionApi}{path}");
        req.Headers.Authorization = new AuthenticationHeaderValue("Bearer", apiKey);
        req.Headers.Add("Notion-Version", NotionVersion);
        if (body is not null)
        {
            req.Content = new StringContent(body, Encoding.UTF8, "application/json");
        }
        using var resp = await _http.SendAsync(req, ct);
        var text = await resp.Content.ReadAsStringAsync(ct);
        if (!resp.IsSuccessStatusCode)
        {
            throw new HttpRequestException($"Notion API {(int)resp.StatusCode}: {Trim(text, 500)}");
        }
        return JsonDocument.Parse(text).RootElement.Clone();
    }

    private static string GetRequired(IReadOnlyDictionary<string, string> creds, string key)
    {
        if (!creds.TryGetValue(key, out var value) || string.IsNullOrWhiteSpace(value))
        {
            throw new InvalidOperationException($"Notion skill: missing credential '{key}'");
        }
        return value;
    }

    private static string Trim(string s, int max) =>
        s.Length <= max ? s : s[..max];
}
