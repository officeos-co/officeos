using System.Text;
using System.Text.Json;
using EnterpriseAgentOs.Infrastructure.Channels.Common;

namespace EnterpriseAgentOs.Infrastructure.Channels;

internal sealed class TelegramAdapter : IChannelAdapter
{
    public string ChannelType => "telegram";
    public int MaxMessageLength => 4096;

    public bool VerifySignature(byte[] rawBody, Dictionary<string, string> config, IDictionary<string, string> headers)
    {
        if (!config.TryGetValue("webhookSecret", out var secret) || string.IsNullOrEmpty(secret))
            return true;

        return headers.TryGetValue("X-Telegram-Bot-Api-Secret-Token", out var tokenHeader) &&
               string.Equals(secret, tokenHeader, StringComparison.Ordinal);
    }

    public ChannelInboundMessage? ParseInbound(JsonElement body)
    {
        // Handle both message and edited_message
        JsonElement message;
        if (body.TryGetProperty("message", out message)) { }
        else if (body.TryGetProperty("edited_message", out message)) { }
        else return null;

        // Extract chat info
        var chatId = message.TryGetProperty("chat", out var chat) && chat.TryGetProperty("id", out var cid)
            ? cid.GetRawText() : "";
        var chatType = chat.TryGetProperty("type", out var ctProp) ? ctProp.GetString() ?? "private" : "private";
        var isGroup = chatType is "group" or "supergroup";

        // Extract sender info
        var fromId = "";
        string? displayName = null;
        if (message.TryGetProperty("from", out var from))
        {
            fromId = from.TryGetProperty("id", out var fid) ? fid.GetRawText() : "";
            var firstName = from.TryGetProperty("first_name", out var fn) ? fn.GetString() ?? "" : "";
            var lastName = from.TryGetProperty("last_name", out var ln) ? ln.GetString() : null;
            displayName = string.IsNullOrEmpty(lastName) ? firstName : $"{firstName} {lastName}";
            if (string.IsNullOrEmpty(displayName)) displayName = null;
        }

        // Extract text (or caption for media messages)
        var text = message.TryGetProperty("text", out var t) ? t.GetString() ?? "" : "";
        if (string.IsNullOrEmpty(text))
            text = message.TryGetProperty("caption", out var cap) ? cap.GetString() ?? "" : "";

        // Extract message ID for dedup
        var msgId = message.TryGetProperty("message_id", out var mid) ? mid.GetRawText() : null;
        var messageId = msgId is not null ? $"{chatId}:{msgId}" : null;

        // Extract media
        List<ChannelMediaAttachment>? media = null;

        if (message.TryGetProperty("photo", out var photos) && photos.ValueKind == JsonValueKind.Array && photos.GetArrayLength() > 0)
        {
            // Last photo is highest resolution
            var photo = photos[photos.GetArrayLength() - 1];
            var fileId = photo.TryGetProperty("file_id", out var fid2) ? fid2.GetString() : null;
            media ??= new();
            media.Add(new ChannelMediaAttachment("image", fileId, "image/jpeg", null));
        }

        if (message.TryGetProperty("document", out var doc))
        {
            var fileId = doc.TryGetProperty("file_id", out var fid2) ? fid2.GetString() : null;
            var mime = doc.TryGetProperty("mime_type", out var mt) ? mt.GetString() : null;
            var fileName = doc.TryGetProperty("file_name", out var fn) ? fn.GetString() : null;
            media ??= new();
            media.Add(new ChannelMediaAttachment("document", fileId, mime, fileName));
        }

        if (message.TryGetProperty("video", out var vid))
        {
            var fileId = vid.TryGetProperty("file_id", out var fid2) ? fid2.GetString() : null;
            var mime = vid.TryGetProperty("mime_type", out var mt) ? mt.GetString() : null;
            media ??= new();
            media.Add(new ChannelMediaAttachment("video", fileId, mime, null));
        }

        if (message.TryGetProperty("voice", out var voice))
        {
            var fileId = voice.TryGetProperty("file_id", out var fid2) ? fid2.GetString() : null;
            var mime = voice.TryGetProperty("mime_type", out var mt) ? mt.GetString() : null;
            media ??= new();
            media.Add(new ChannelMediaAttachment("audio", fileId, mime, null));
        }

        if (message.TryGetProperty("audio", out var audio))
        {
            var fileId = audio.TryGetProperty("file_id", out var fid2) ? fid2.GetString() : null;
            var mime = audio.TryGetProperty("mime_type", out var mt) ? mt.GetString() : null;
            var fileName = audio.TryGetProperty("file_name", out var fn) ? fn.GetString() : null;
            media ??= new();
            media.Add(new ChannelMediaAttachment("audio", fileId, mime, fileName));
        }

        // Require text or media
        if (string.IsNullOrEmpty(text) && (media is null || media.Count == 0)) return null;

        return new ChannelInboundMessage(
            fromId, chatId, text,
            MessageId: messageId,
            IsGroupMessage: isGroup,
            GroupId: isGroup ? chatId : null,
            SenderDisplayName: displayName,
            Media: media);
    }

    public async Task SendReplyAsync(HttpClient client, Dictionary<string, string> config, string channelId, string text, CancellationToken ct = default)
    {
        if (!config.TryGetValue("botToken", out var token) || string.IsNullOrEmpty(token)) return;

        var converted = MarkdownFormatConverter.Convert(text, "telegram");
        var chunks = PlatformTextChunker.ChunkForPlatform(converted, "telegram");

        foreach (var chunk in chunks)
        {
            var payload = JsonSerializer.Serialize(new { chat_id = channelId, text = chunk, parse_mode = "MarkdownV2" });
            var content = new StringContent(payload, Encoding.UTF8, "application/json");
            var response = await client.PostAsync($"https://api.telegram.org/bot{token}/sendMessage", content, ct);

            // If MarkdownV2 fails (malformed), retry without parse_mode
            if (!response.IsSuccessStatusCode)
            {
                var fallbackPayload = JsonSerializer.Serialize(new { chat_id = channelId, text = chunk });
                var fallbackContent = new StringContent(fallbackPayload, Encoding.UTF8, "application/json");
                await client.PostAsync($"https://api.telegram.org/bot{token}/sendMessage", fallbackContent, ct);
            }
        }
    }
}
