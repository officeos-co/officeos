using System.Security.Cryptography;

namespace EnterpriseAgentOs.Infrastructure.Adapters.Channels.Adapters;

public sealed class WhatsAppAdapter : IChannelAdapter
{
    public string ChannelType => "whatsapp";

    public bool VerifySignature(byte[] rawBody, Dictionary<string, string> config, IDictionary<string, string> headers)
    {
        // WhatsApp/Meta uses X-Hub-Signature-256 (HMAC-SHA256 of app secret)
        if (!config.TryGetValue("accessToken", out var secret) || string.IsNullOrEmpty(secret))
            return true; // no secret = trust during initial setup

        if (!headers.TryGetValue("X-Hub-Signature-256", out var expected))
            return false;

        using var hmac = new HMACSHA256(Encoding.UTF8.GetBytes(secret));
        var hash = hmac.ComputeHash(rawBody);
        var computed = "sha256=" + Convert.ToHexString(hash).ToLowerInvariant();

        return string.Equals(computed, expected, StringComparison.Ordinal);
    }

    public ChannelInboundMessage? ParseInbound(JsonElement body)
    {
        if (!body.TryGetProperty("entry", out var entries) || entries.ValueKind != JsonValueKind.Array)
            return null;

        foreach (var entry in entries.EnumerateArray())
        {
            if (!entry.TryGetProperty("changes", out var changes) || changes.ValueKind != JsonValueKind.Array)
                continue;

            foreach (var change in changes.EnumerateArray())
            {
                if (!change.TryGetProperty("value", out var value)) continue;
                if (!value.TryGetProperty("messages", out var messages) || messages.ValueKind != JsonValueKind.Array)
                    continue;

                foreach (var msg in messages.EnumerateArray())
                {
                    var msgType = msg.TryGetProperty("type", out var mt) ? mt.GetString() : null;
                    if (msgType != "text") continue;

                    var text = msg.TryGetProperty("text", out var textObj) &&
                               textObj.TryGetProperty("body", out var textBody)
                        ? textBody.GetString() ?? "" : "";

                    var from = msg.TryGetProperty("from", out var f) ? f.GetString() ?? "" : "";

                    var phoneNumberId = value.TryGetProperty("metadata", out var meta) &&
                                        meta.TryGetProperty("phone_number_id", out var pnid)
                        ? pnid.GetString() ?? "" : "";

                    if (!string.IsNullOrEmpty(text))
                        return new ChannelInboundMessage(from, $"{phoneNumberId}:{from}", text);
                }
            }
        }

        return null;
    }

    public async Task SendReplyAsync(HttpClient client, Dictionary<string, string> config, string channelId, string text, CancellationToken ct = default)
    {
        if (!config.TryGetValue("accessToken", out var token) || string.IsNullOrEmpty(token)) return;

        var parts = channelId.Split(':', 2);
        var phoneNumberId = config.TryGetValue("phoneNumberId", out var pnid) ? pnid : (parts.Length == 2 ? parts[0] : "");
        var recipientPhone = parts.Length == 2 ? parts[1] : channelId;

        if (string.IsNullOrEmpty(phoneNumberId)) return;

        var payload = JsonSerializer.Serialize(new
        {
            messaging_product = "whatsapp",
            to = recipientPhone,
            type = "text",
            text = new { body = text },
        });

        var request = new HttpRequestMessage(HttpMethod.Post,
            $"https://graph.facebook.com/v19.0/{phoneNumberId}/messages")
        {
            Content = new StringContent(payload, Encoding.UTF8, "application/json"),
        };
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
        await client.SendAsync(request, ct);
    }
}
