using System.Security.Cryptography;

namespace EnterpriseAgentOs.Infrastructure.Adapters;

internal sealed class GoogleChatAdapter : IChannelAdapter
{
    public string ChannelType => "google-chat";

    /// <summary>
    /// Google Chat sends a Bearer JWT signed by Google's service account.
    /// We validate the basic structure, issuer, and expiry.
    /// </summary>
    public bool VerifySignature(byte[] rawBody, Dictionary<string, string> config, IDictionary<string, string> headers)
    {
        if (!headers.TryGetValue("Authorization", out var authHeader))
            return false;

        if (!authHeader.StartsWith("Bearer ", StringComparison.OrdinalIgnoreCase))
            return false;

        var token = authHeader["Bearer ".Length..];

        try
        {
            var parts = token.Split('.');
            if (parts.Length != 3) return false;

            var payloadJson = Encoding.UTF8.GetString(DecodeBase64Url(parts[1]));
            var payload = JsonSerializer.Deserialize<JsonElement>(payloadJson);

            var issuer = payload.TryGetProperty("iss", out var iss) ? iss.GetString() ?? "" : "";
            var validIssuers = new[] { "chat@system.gserviceaccount.com", "https://accounts.google.com" };
            if (!validIssuers.Any(vi => string.Equals(vi, issuer, StringComparison.OrdinalIgnoreCase)))
                return false;

            if (payload.TryGetProperty("exp", out var exp))
            {
                var expTime = DateTimeOffset.FromUnixTimeSeconds(exp.GetInt64());
                if (expTime < DateTimeOffset.UtcNow) return false;
            }

            return true;
        }
        catch
        {
            return false;
        }
    }

    public ChannelInboundMessage? ParseInbound(JsonElement body)
    {
        var eventType = body.TryGetProperty("type", out var t) ? t.GetString() : null;
        if (eventType == "ADDED_TO_SPACE") return null;
        if (eventType != "MESSAGE") return null;

        if (!body.TryGetProperty("message", out var message)) return null;

        // Prefer argumentText (without @mention) over text
        var text = message.TryGetProperty("argumentText", out var argText)
            ? argText.GetString()?.Trim() ?? ""
            : message.TryGetProperty("text", out var txt) ? txt.GetString() ?? "" : "";

        if (string.IsNullOrEmpty(text)) return null;

        var senderId = body.TryGetProperty("user", out var user) && user.TryGetProperty("name", out var uname)
            ? uname.GetString() ?? "" : "";

        var spaceName = body.TryGetProperty("space", out var space) && space.TryGetProperty("name", out var sname)
            ? sname.GetString() ?? "" : "";

        var threadName = message.TryGetProperty("thread", out var thread) && thread.TryGetProperty("name", out var tname)
            ? tname.GetString() ?? "" : "";

        var channelId = string.IsNullOrEmpty(threadName) ? spaceName : $"{spaceName}|{threadName}";

        return new ChannelInboundMessage(senderId, channelId, text);
    }

    public async Task SendReplyAsync(HttpClient client, Dictionary<string, string> config, string channelId, string text, CancellationToken ct = default)
    {
        if (!config.TryGetValue("serviceAccountJson", out var saJson) || string.IsNullOrEmpty(saJson))
            return;

        var parts = channelId.Split('|', 2);
        var spaceName = parts[0];
        var threadName = parts.Length > 1 ? parts[1] : null;

        var accessToken = await GetServiceAccountTokenAsync(client, saJson, ct);
        if (string.IsNullOrEmpty(accessToken)) return;

        var messageObj = new Dictionary<string, object> { ["text"] = text };
        if (!string.IsNullOrEmpty(threadName))
            messageObj["thread"] = new { name = threadName };

        var payload = JsonSerializer.Serialize(messageObj);
        var url = $"https://chat.googleapis.com/v1/{spaceName}/messages";
        var request = new HttpRequestMessage(HttpMethod.Post, url)
        {
            Content = new StringContent(payload, Encoding.UTF8, "application/json"),
        };
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
        await client.SendAsync(request, ct);
    }

    private static async Task<string?> GetServiceAccountTokenAsync(HttpClient client, string serviceAccountJson, CancellationToken ct)
    {
        try
        {
            var sa = JsonSerializer.Deserialize<JsonElement>(serviceAccountJson);
            var clientEmail = sa.GetProperty("client_email").GetString()!;
            var privateKeyPem = sa.GetProperty("private_key").GetString()!;

            var now = DateTimeOffset.UtcNow;
            var header = EncodeBase64Url(Encoding.UTF8.GetBytes(
                JsonSerializer.Serialize(new { alg = "RS256", typ = "JWT" })));
            var claims = EncodeBase64Url(Encoding.UTF8.GetBytes(
                JsonSerializer.Serialize(new
                {
                    iss = clientEmail,
                    scope = "https://www.googleapis.com/auth/chat.bot",
                    aud = "https://oauth2.googleapis.com/token",
                    iat = now.ToUnixTimeSeconds(),
                    exp = now.AddMinutes(30).ToUnixTimeSeconds(),
                })));

            var unsignedToken = $"{header}.{claims}";

            var keyData = privateKeyPem
                .Replace("-----BEGIN PRIVATE KEY-----", "")
                .Replace("-----END PRIVATE KEY-----", "")
                .Replace("\n", "");
            using var rsa = RSA.Create();
            rsa.ImportPkcs8PrivateKey(Convert.FromBase64String(keyData), out _);

            var signature = rsa.SignData(
                Encoding.UTF8.GetBytes(unsignedToken),
                HashAlgorithmName.SHA256,
                RSASignaturePadding.Pkcs1);
            var sig = EncodeBase64Url(signature);

            var jwt = $"{unsignedToken}.{sig}";

            var form = new FormUrlEncodedContent(new[]
            {
                new KeyValuePair<string, string>("grant_type", "urn:ietf:params:oauth:grant-type:jwt-bearer"),
                new KeyValuePair<string, string>("assertion", jwt),
            });

            var response = await client.PostAsync("https://oauth2.googleapis.com/token", form, ct);
            if (!response.IsSuccessStatusCode) return null;

            var body = await response.Content.ReadAsStringAsync(ct);
            var json = JsonSerializer.Deserialize<JsonElement>(body);
            return json.TryGetProperty("access_token", out var token) ? token.GetString() : null;
        }
        catch
        {
            return null;
        }
    }

    private static string EncodeBase64Url(byte[] data)
        => Convert.ToBase64String(data).TrimEnd('=').Replace('+', '-').Replace('/', '_');

    private static byte[] DecodeBase64Url(string input)
    {
        var s = input.Replace('-', '+').Replace('_', '/');
        switch (s.Length % 4)
        {
            case 2: s += "=="; break;
            case 3: s += "="; break;
        }
        return Convert.FromBase64String(s);
    }
}
