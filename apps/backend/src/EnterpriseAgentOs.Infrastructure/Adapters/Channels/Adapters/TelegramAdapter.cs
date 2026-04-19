namespace EnterpriseAgentOs.Infrastructure.Adapters.Channels.Adapters;

public sealed class TelegramAdapter : IChannelAdapter
{
    public string ChannelType => "telegram";

    public bool VerifySignature(byte[] rawBody, Dictionary<string, string> config, IDictionary<string, string> headers)
    {
        if (!config.TryGetValue("webhookSecret", out var secret) || string.IsNullOrEmpty(secret))
            return true; // no secret = trust

        return headers.TryGetValue("X-Telegram-Bot-Api-Secret-Token", out var tokenHeader) &&
               string.Equals(secret, tokenHeader, StringComparison.Ordinal);
    }

    public ChannelInboundMessage? ParseInbound(JsonElement body)
    {
        if (!body.TryGetProperty("message", out var message)) return null;

        var text = message.TryGetProperty("text", out var t) ? t.GetString() ?? "" : "";
        if (string.IsNullOrEmpty(text)) return null;

        var chatId = message.TryGetProperty("chat", out var chat) && chat.TryGetProperty("id", out var cid)
            ? cid.GetRawText() : "";
        var fromId = message.TryGetProperty("from", out var from) && from.TryGetProperty("id", out var fid)
            ? fid.GetRawText() : "";

        return new ChannelInboundMessage(fromId, chatId, text);
    }

    public async Task SendReplyAsync(HttpClient client, Dictionary<string, string> config, string channelId, string text, CancellationToken ct = default)
    {
        if (!config.TryGetValue("botToken", out var token) || string.IsNullOrEmpty(token)) return;

        var payload = JsonSerializer.Serialize(new { chat_id = channelId, text });
        var content = new StringContent(payload, Encoding.UTF8, "application/json");
        await client.PostAsync($"https://api.telegram.org/bot{token}/sendMessage", content, ct);
    }
}
