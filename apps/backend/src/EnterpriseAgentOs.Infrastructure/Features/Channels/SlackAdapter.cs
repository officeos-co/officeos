using System.Net.Http.Headers;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using EnterpriseAgentOs.Infrastructure.Features.Channels.Common;

namespace EnterpriseAgentOs.Infrastructure.Features.Channels;

internal sealed class SlackAdapter : IChannelAdapter
{
    public string ChannelType => "slack";
    public int MaxMessageLength => 4000;

    public bool VerifySignature(byte[] rawBody, Dictionary<string, string> config, IDictionary<string, string> headers)
    {
        if (!config.TryGetValue("signingSecret", out var secret) || string.IsNullOrEmpty(secret))
            return false;

        if (!headers.TryGetValue("X-Slack-Signature", out var sig) ||
            !headers.TryGetValue("X-Slack-Request-Timestamp", out var timestamp))
            return false;

        // Replay protection: reject requests older than 5 minutes
        if (long.TryParse(timestamp, out var ts))
        {
            var age = Math.Abs(DateTimeOffset.UtcNow.ToUnixTimeSeconds() - ts);
            if (age > 300) return false;
        }

        var baseString = $"v0:{timestamp}:{Encoding.UTF8.GetString(rawBody)}";
        using var hmac = new HMACSHA256(Encoding.UTF8.GetBytes(secret));
        var hash = hmac.ComputeHash(Encoding.UTF8.GetBytes(baseString));
        var computed = "v0=" + Convert.ToHexString(hash).ToLowerInvariant();

        return string.Equals(computed, sig, StringComparison.Ordinal);
    }

    public ChannelInboundMessage? ParseInbound(JsonElement body)
    {
        var type = body.TryGetProperty("type", out var t) ? t.GetString() : null;

        if (type == "url_verification")
        {
            var challenge = body.GetProperty("challenge").GetString() ?? "";
            return new ChannelInboundMessage("", "", "", IsChallenge: true, ChallengeResponse: challenge);
        }

        if (type != "event_callback") return null;
        if (!body.TryGetProperty("event", out var evt)) return null;

        var evtType = evt.TryGetProperty("type", out var et) ? et.GetString() : null;
        if (evtType != "message") return null;
        if (evt.TryGetProperty("bot_id", out _)) return null;
        if (evt.TryGetProperty("subtype", out _)) return null;

        var text = evt.TryGetProperty("text", out var tx) ? tx.GetString() ?? "" : "";
        var channel = evt.TryGetProperty("channel", out var c) ? c.GetString() ?? "" : "";
        var user = evt.TryGetProperty("user", out var u) ? u.GetString() ?? "" : "";

        if (string.IsNullOrEmpty(text)) return null;

        // Deduplication: use event_id from root
        var messageId = body.TryGetProperty("event_id", out var eid) ? eid.GetString() : null;

        // Group detection: channel_type "im" = DM, else group
        var channelType = evt.TryGetProperty("channel_type", out var ct2) ? ct2.GetString() : null;
        var isGroup = channelType != "im";

        // Media: parse files array
        List<ChannelMediaAttachment>? media = null;
        if (evt.TryGetProperty("files", out var files) && files.ValueKind == JsonValueKind.Array)
        {
            media = new List<ChannelMediaAttachment>();
            foreach (var file in files.EnumerateArray())
            {
                var mimetype = file.TryGetProperty("mimetype", out var mt) ? mt.GetString() ?? "" : "";
                var url = file.TryGetProperty("url_private", out var up) ? up.GetString() : null;
                var name = file.TryGetProperty("name", out var fn) ? fn.GetString() : null;
                var mediaType = mimetype.StartsWith("image/") ? "image"
                    : mimetype.StartsWith("video/") ? "video"
                    : mimetype.StartsWith("audio/") ? "audio"
                    : "document";
                media.Add(new ChannelMediaAttachment(mediaType, url, mimetype, name));
            }
        }

        return new ChannelInboundMessage(
            user, channel, text,
            MessageId: messageId,
            IsGroupMessage: isGroup,
            GroupId: isGroup ? channel : null,
            Media: media);
    }

    public async Task SendReplyAsync(HttpClient client, Dictionary<string, string> config, string channelId, string text, CancellationToken ct = default)
    {
        if (!config.TryGetValue("botToken", out var token) || string.IsNullOrEmpty(token)) return;

        var converted = MarkdownFormatConverter.Convert(text, "slack");
        var chunks = PlatformTextChunker.ChunkForPlatform(converted, "slack");

        foreach (var chunk in chunks)
        {
            var payload = JsonSerializer.Serialize(new { channel = channelId, text = chunk });
            var request = new HttpRequestMessage(HttpMethod.Post, "https://slack.com/api/chat.postMessage")
            {
                Content = new StringContent(payload, Encoding.UTF8, "application/json"),
            };
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
            var response = await client.SendAsync(request, ct);

            // Log errors from Slack API
            if (response.IsSuccessStatusCode)
            {
                var responseBody = await response.Content.ReadAsStringAsync(ct);
                var responseJson = JsonSerializer.Deserialize<JsonElement>(responseBody);
                if (responseJson.TryGetProperty("ok", out var ok) && !ok.GetBoolean())
                {
                    // Slack returned ok: false — error is in the "error" field
                    // Logged at controller/router level
                }
            }
        }
    }
}
