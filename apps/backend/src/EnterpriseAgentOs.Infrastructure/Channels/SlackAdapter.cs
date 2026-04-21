using System.Security.Cryptography;

namespace EnterpriseAgentOs.Infrastructure.Channels;

internal sealed class SlackAdapter : IChannelAdapter
{
    public string ChannelType => "slack";

    public bool VerifySignature(byte[] rawBody, Dictionary<string, string> config, IDictionary<string, string> headers)
    {
        if (!config.TryGetValue("signingSecret", out var secret) || string.IsNullOrEmpty(secret))
            return false;

        if (!headers.TryGetValue("X-Slack-Signature", out var sig) ||
            !headers.TryGetValue("X-Slack-Request-Timestamp", out var timestamp))
            return false;

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
        return new ChannelInboundMessage(user, channel, text);
    }

    public async Task SendReplyAsync(HttpClient client, Dictionary<string, string> config, string channelId, string text, CancellationToken ct = default)
    {
        if (!config.TryGetValue("botToken", out var token) || string.IsNullOrEmpty(token)) return;

        var payload = JsonSerializer.Serialize(new { channel = channelId, text });
        var request = new HttpRequestMessage(HttpMethod.Post, "https://slack.com/api/chat.postMessage")
        {
            Content = new StringContent(payload, Encoding.UTF8, "application/json"),
        };
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
        await client.SendAsync(request, ct);
    }
}
