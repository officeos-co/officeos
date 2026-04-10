using System.Net.Http.Headers;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;

namespace EnterpriseAgentOs.Api.Entities.Skills.Implementations;

public sealed record GoogleDriveSearchRequest(string Query, int PageSize = 10);
public sealed record GoogleCalendarUpcomingRequest(int MaxResults = 10);

public sealed class GoogleSkill
{
    private const string DriveApi = "https://www.googleapis.com/drive/v3";
    private const string CalendarApi = "https://www.googleapis.com/calendar/v3";
    private const string TokenUrl = "https://oauth2.googleapis.com/token";

    private readonly HttpClient _http;

    public GoogleSkill(HttpClient http)
    {
        _http = http;
    }

    public async Task<object> DriveSearchAsync(
        GoogleDriveSearchRequest req,
        IReadOnlyDictionary<string, string> creds,
        CancellationToken ct = default)
    {
        var sa = ParseServiceAccount(creds);
        var token = await AccessTokenAsync(
            sa,
            new[] { "https://www.googleapis.com/auth/drive.readonly" },
            ct);

        var qEscaped = req.Query.Replace("'", "\\'");
        var url = $"{DriveApi}/files"
            + $"?q={Uri.EscapeDataString($"name contains '{qEscaped}' and trashed = false")}"
            + $"&pageSize={req.PageSize}"
            + "&fields=" + Uri.EscapeDataString("files(id,name,mimeType,webViewLink,modifiedTime)");

        var data = await CallGoogleAsync(HttpMethod.Get, url, token, ct);
        var files = new List<JsonElement>();
        if (data.TryGetProperty("files", out var arr) && arr.ValueKind == JsonValueKind.Array)
        {
            foreach (var f in arr.EnumerateArray()) files.Add(f.Clone());
        }
        return new { files };
    }

    public async Task<object> CalendarUpcomingAsync(
        GoogleCalendarUpcomingRequest req,
        IReadOnlyDictionary<string, string> creds,
        CancellationToken ct = default)
    {
        var sa = ParseServiceAccount(creds);
        var token = await AccessTokenAsync(
            sa,
            new[] { "https://www.googleapis.com/auth/calendar.readonly" },
            ct);

        var calendarId = creds.TryGetValue("calendar_id", out var cid) && !string.IsNullOrWhiteSpace(cid)
            ? cid
            : "primary";
        var nowIso = DateTime.UtcNow.ToString("yyyy-MM-ddTHH:mm:ssZ");
        var url = $"{CalendarApi}/calendars/{Uri.EscapeDataString(calendarId)}/events"
            + $"?maxResults={req.MaxResults}"
            + "&orderBy=startTime"
            + "&singleEvents=true"
            + $"&timeMin={Uri.EscapeDataString(nowIso)}";

        var data = await CallGoogleAsync(HttpMethod.Get, url, token, ct);
        var events = new List<object>();
        if (data.TryGetProperty("items", out var items) && items.ValueKind == JsonValueKind.Array)
        {
            foreach (var e in items.EnumerateArray())
            {
                events.Add(new
                {
                    id = Prop(e, "id"),
                    summary = Prop(e, "summary"),
                    start = ExtractDateOrDateTime(e, "start"),
                    end = ExtractDateOrDateTime(e, "end"),
                    html_link = Prop(e, "htmlLink"),
                });
            }
        }
        return new { events };
    }

    private static string? ExtractDateOrDateTime(JsonElement parent, string key)
    {
        if (!parent.TryGetProperty(key, out var el) || el.ValueKind != JsonValueKind.Object) return null;
        if (el.TryGetProperty("dateTime", out var dt) && dt.ValueKind == JsonValueKind.String) return dt.GetString();
        if (el.TryGetProperty("date", out var d) && d.ValueKind == JsonValueKind.String) return d.GetString();
        return null;
    }

    private static JsonElement ParseServiceAccount(IReadOnlyDictionary<string, string> creds)
    {
        if (!creds.TryGetValue("service_account_json", out var raw) || string.IsNullOrWhiteSpace(raw))
        {
            throw new InvalidOperationException("Google skill: service_account_json not configured.");
        }
        try
        {
            return JsonDocument.Parse(raw).RootElement.Clone();
        }
        catch (JsonException ex)
        {
            throw new InvalidOperationException(
                $"Google skill: service_account_json is not valid JSON: {ex.Message}", ex);
        }
    }

    private async Task<string> AccessTokenAsync(JsonElement sa, string[] scopes, CancellationToken ct)
    {
        var clientEmail = sa.TryGetProperty("client_email", out var ce) ? ce.GetString() : null;
        var privateKeyPem = sa.TryGetProperty("private_key", out var pk) ? pk.GetString() : null;
        if (string.IsNullOrEmpty(clientEmail) || string.IsNullOrEmpty(privateKeyPem))
        {
            throw new InvalidOperationException("Google skill: service_account_json missing client_email or private_key.");
        }

        var now = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
        var header = JsonSerializer.Serialize(new { alg = "RS256", typ = "JWT" });
        var claims = JsonSerializer.Serialize(new
        {
            iss = clientEmail,
            scope = string.Join(" ", scopes),
            aud = TokenUrl,
            iat = now,
            exp = now + 3600,
        });

        var headerB64 = Base64UrlEncode(Encoding.UTF8.GetBytes(header));
        var claimsB64 = Base64UrlEncode(Encoding.UTF8.GetBytes(claims));
        var signingInput = $"{headerB64}.{claimsB64}";

        using var rsa = RSA.Create();
        rsa.ImportFromPem(privateKeyPem);
        var signature = rsa.SignData(
            Encoding.ASCII.GetBytes(signingInput),
            HashAlgorithmName.SHA256,
            RSASignaturePadding.Pkcs1);
        var assertion = $"{signingInput}.{Base64UrlEncode(signature)}";

        using var req = new HttpRequestMessage(HttpMethod.Post, TokenUrl);
        req.Content = new FormUrlEncodedContent(new Dictionary<string, string>
        {
            ["grant_type"] = "urn:ietf:params:oauth:grant-type:jwt-bearer",
            ["assertion"] = assertion,
        });
        using var resp = await _http.SendAsync(req, ct);
        var body = await resp.Content.ReadAsStringAsync(ct);
        if (!resp.IsSuccessStatusCode)
        {
            throw new HttpRequestException($"Google token exchange {(int)resp.StatusCode}: {Trim(body, 500)}");
        }
        var parsed = JsonDocument.Parse(body).RootElement;
        if (!parsed.TryGetProperty("access_token", out var at) || at.ValueKind != JsonValueKind.String)
        {
            throw new HttpRequestException("Google token exchange: no access_token in response");
        }
        return at.GetString()!;
    }

    private async Task<JsonElement> CallGoogleAsync(
        HttpMethod method,
        string url,
        string accessToken,
        CancellationToken ct)
    {
        using var req = new HttpRequestMessage(method, url);
        req.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
        using var resp = await _http.SendAsync(req, ct);
        var text = await resp.Content.ReadAsStringAsync(ct);
        if (!resp.IsSuccessStatusCode)
        {
            throw new HttpRequestException($"Google API {(int)resp.StatusCode}: {Trim(text, 500)}");
        }
        return JsonDocument.Parse(text).RootElement.Clone();
    }

    private static string Base64UrlEncode(byte[] data) =>
        Convert.ToBase64String(data).TrimEnd('=').Replace('+', '-').Replace('/', '_');

    private static string? Prop(JsonElement e, string key) =>
        e.TryGetProperty(key, out var v) && v.ValueKind == JsonValueKind.String ? v.GetString() : null;

    private static string Trim(string s, int max) => s.Length <= max ? s : s[..max];
}
