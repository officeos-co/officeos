using System.Collections.Concurrent;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using EnterpriseAgentOs.Infrastructure.Channels.Common;

namespace EnterpriseAgentOs.Infrastructure.Channels;

internal sealed class TeamsAdapter : IChannelAdapter
{
    public string ChannelType => "teams";
    public int MaxMessageLength => 28000;

    private static readonly ConcurrentDictionary<string, (string Token, DateTimeOffset Expiry)> TokenCache = new();

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
            var parts = token.Split('.');
            if (parts.Length != 3) return false;

            var payloadJson = Encoding.UTF8.GetString(DecodeBase64Url(parts[1]));
            var payload = JsonSerializer.Deserialize<JsonElement>(payloadJson);

            var issuer = payload.TryGetProperty("iss", out var iss) ? iss.GetString() ?? "" : "";
            var validIssuers = new[]
            {
                "https://api.botframework.com",
                "https://sts.windows.net/d6d49420-f39b-4df7-a1dc-d59a935871db/",
                "https://login.microsoftonline.com/d6d49420-f39b-4df7-a1dc-d59a935871db/v2.0",
            };
            if (!validIssuers.Any(vi => string.Equals(vi, issuer, StringComparison.OrdinalIgnoreCase)))
                return false;

            var aud = payload.TryGetProperty("aud", out var a) ? a.GetString() ?? "" : "";
            if (!string.Equals(aud, appId, StringComparison.OrdinalIgnoreCase))
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
        var activityType = body.TryGetProperty("type", out var t) ? t.GetString() : null;
        if (activityType != "message") return null;

        var text = body.TryGetProperty("text", out var txt) ? txt.GetString() ?? "" : "";
        if (string.IsNullOrEmpty(text)) return null;

        var fromId = "";
        string? displayName = null;
        if (body.TryGetProperty("from", out var from))
        {
            fromId = from.TryGetProperty("id", out var fid) ? fid.GetString() ?? "" : "";
            displayName = from.TryGetProperty("name", out var fn) ? fn.GetString() : null;
        }

        var conversationId = "";
        var isGroup = false;
        if (body.TryGetProperty("conversation", out var conv))
        {
            conversationId = conv.TryGetProperty("id", out var cid) ? cid.GetString() ?? "" : "";
            var convType = conv.TryGetProperty("conversationType", out var ct) ? ct.GetString() : null;
            isGroup = convType != "personal";
        }

        var serviceUrl = body.TryGetProperty("serviceUrl", out var su) ? su.GetString() ?? "" : "";

        // Deduplication: use activity id
        var messageId = body.TryGetProperty("id", out var mid) ? mid.GetString() : null;

        // Parse attachments
        List<ChannelMediaAttachment>? media = null;
        if (body.TryGetProperty("attachments", out var attachments) && attachments.ValueKind == JsonValueKind.Array)
        {
            media = new();
            foreach (var att in attachments.EnumerateArray())
            {
                var contentType = att.TryGetProperty("contentType", out var ct2) ? ct2.GetString() ?? "" : "";
                var contentUrl = att.TryGetProperty("contentUrl", out var cu) ? cu.GetString() : null;
                var name = att.TryGetProperty("name", out var n) ? n.GetString() : null;
                var mediaType = contentType.StartsWith("image/") ? "image"
                    : contentType.StartsWith("video/") ? "video"
                    : contentType.StartsWith("audio/") ? "audio"
                    : "document";
                media.Add(new ChannelMediaAttachment(mediaType, contentUrl, contentType, name));
            }
        }

        return new ChannelInboundMessage(
            fromId, $"{serviceUrl}|{conversationId}", text,
            MessageId: messageId,
            IsGroupMessage: isGroup,
            GroupId: isGroup ? conversationId : null,
            SenderDisplayName: displayName,
            Media: media);
    }

    public async Task SendReplyAsync(HttpClient client, Dictionary<string, string> config, string channelId, string text, CancellationToken ct = default)
    {
        if (!config.TryGetValue("appId", out var appId) || !config.TryGetValue("appSecret", out var appSecret))
            return;

        var parts = channelId.Split('|', 2);
        if (parts.Length != 2) return;
        var serviceUrl = parts[0];
        var conversationId = parts[1];

        var botToken = await GetCachedBotTokenAsync(client, appId, appSecret, ct);
        if (string.IsNullOrEmpty(botToken)) return;

        var chunks = PlatformTextChunker.ChunkForPlatform(text, "teams");

        foreach (var chunk in chunks)
        {
            var activity = JsonSerializer.Serialize(new { type = "message", text = chunk, textFormat = "markdown" });
            var url = $"{serviceUrl.TrimEnd('/')}/v3/conversations/{conversationId}/activities";
            var request = new HttpRequestMessage(HttpMethod.Post, url)
            {
                Content = new StringContent(activity, Encoding.UTF8, "application/json"),
            };
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", botToken);
            await client.SendAsync(request, ct);
        }
    }

    private static async Task<string?> GetCachedBotTokenAsync(HttpClient client, string appId, string appSecret, CancellationToken ct)
    {
        var cacheKey = appId;

        if (TokenCache.TryGetValue(cacheKey, out var cached) && cached.Expiry > DateTimeOffset.UtcNow.AddMinutes(2))
            return cached.Token;

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
        var token = json.TryGetProperty("access_token", out var t) ? t.GetString() : null;
        var expiresIn = json.TryGetProperty("expires_in", out var ei) ? ei.GetInt32() : 3600;

        if (token is not null)
            TokenCache[cacheKey] = (token, DateTimeOffset.UtcNow.AddSeconds(expiresIn));

        return token;
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
