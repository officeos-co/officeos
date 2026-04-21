namespace EnterpriseAgentOs.Infrastructure.Adapters;

internal sealed class TeamsAdapter : IChannelAdapter
{
    public string ChannelType => "teams";

    /// <summary>
    /// Microsoft Bot Framework sends a JWT Bearer token in the Authorization header.
    /// We validate the basic structure, issuer, audience, and expiry without full
    /// JWKS verification (which requires fetching Microsoft's public keys).
    /// </summary>
    public bool VerifySignature(byte[] rawBody, Dictionary<string, string> config, IDictionary<string, string> headers)
    {
        if (!config.TryGetValue("appId", out var appId) || string.IsNullOrEmpty(appId))
            return false;

        if (!headers.TryGetValue("Authorization", out var authHeader))
            return false;

        if (!authHeader.StartsWith("Bearer ", StringComparison.OrdinalIgnoreCase))
            return false;

        var token = authHeader["Bearer ".Length..];

        try
        {
            // Decode JWT payload (base64url)
            var parts = token.Split('.');
            if (parts.Length != 3) return false;

            var payloadJson = Encoding.UTF8.GetString(DecodeBase64Url(parts[1]));
            var payload = JsonSerializer.Deserialize<JsonElement>(payloadJson);

            // Validate issuer
            var issuer = payload.TryGetProperty("iss", out var iss) ? iss.GetString() ?? "" : "";
            var validIssuers = new[]
            {
                "https://api.botframework.com",
                "https://sts.windows.net/d6d49420-f39b-4df7-a1dc-d59a935871db/",
                "https://login.microsoftonline.com/d6d49420-f39b-4df7-a1dc-d59a935871db/v2.0",
            };
            if (!validIssuers.Any(vi => string.Equals(vi, issuer, StringComparison.OrdinalIgnoreCase)))
                return false;

            // Validate audience matches app ID
            var aud = payload.TryGetProperty("aud", out var a) ? a.GetString() ?? "" : "";
            if (!string.Equals(aud, appId, StringComparison.OrdinalIgnoreCase))
                return false;

            // Check expiry
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
        var activityType = body.TryGetProperty("type", out var t) ? t.GetString() : null;
        if (activityType != "message") return null;

        var text = body.TryGetProperty("text", out var txt) ? txt.GetString() ?? "" : "";
        if (string.IsNullOrEmpty(text)) return null;

        var fromId = body.TryGetProperty("from", out var from) && from.TryGetProperty("id", out var fid)
            ? fid.GetString() ?? "" : "";

        var conversationId = body.TryGetProperty("conversation", out var conv) && conv.TryGetProperty("id", out var cid)
            ? cid.GetString() ?? "" : "";

        var serviceUrl = body.TryGetProperty("serviceUrl", out var su) ? su.GetString() ?? "" : "";

        // Encode both for SendReplyAsync
        return new ChannelInboundMessage(fromId, $"{serviceUrl}|{conversationId}", text);
    }

    public async Task SendReplyAsync(HttpClient client, Dictionary<string, string> config, string channelId, string text, CancellationToken ct = default)
    {
        if (!config.TryGetValue("appId", out var appId) || !config.TryGetValue("appSecret", out var appSecret))
            return;

        var parts = channelId.Split('|', 2);
        if (parts.Length != 2) return;
        var serviceUrl = parts[0];
        var conversationId = parts[1];

        var botToken = await GetBotTokenAsync(client, appId, appSecret, ct);
        if (string.IsNullOrEmpty(botToken)) return;

        var activity = JsonSerializer.Serialize(new { type = "message", text });
        var url = $"{serviceUrl.TrimEnd('/')}/v3/conversations/{conversationId}/activities";
        var request = new HttpRequestMessage(HttpMethod.Post, url)
        {
            Content = new StringContent(activity, Encoding.UTF8, "application/json"),
        };
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", botToken);
        await client.SendAsync(request, ct);
    }

    private static async Task<string?> GetBotTokenAsync(HttpClient client, string appId, string appSecret, CancellationToken ct)
    {
        var form = new FormUrlEncodedContent(new[]
        {
            new KeyValuePair<string, string>("grant_type", "client_credentials"),
            new KeyValuePair<string, string>("client_id", appId),
            new KeyValuePair<string, string>("client_secret", appSecret),
            new KeyValuePair<string, string>("scope", "https://api.botframework.com/.default"),
        });

        var response = await client.PostAsync(
            "https://login.microsoftonline.com/botframework.com/oauth2/v2.0/token", form, ct);
        if (!response.IsSuccessStatusCode) return null;

        var body = await response.Content.ReadAsStringAsync(ct);
        var json = JsonSerializer.Deserialize<JsonElement>(body);
        return json.TryGetProperty("access_token", out var token) ? token.GetString() : null;
    }

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
