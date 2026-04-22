using System.Net.Http.Headers;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using EnterpriseAgentOs.Infrastructure.Channels.Common;

namespace EnterpriseAgentOs.Infrastructure.Channels;

internal sealed class DiscordAdapter : IChannelAdapter
{
    public string ChannelType => "discord";
    public int MaxMessageLength => 2000;

    public bool VerifySignature(byte[] rawBody, Dictionary<string, string> config, IDictionary<string, string> headers)
    {
        if (!config.TryGetValue("publicKey", out var publicKeyHex) || string.IsNullOrEmpty(publicKeyHex))
            return false;

        if (!headers.TryGetValue("X-Signature-Ed25519", out var sigHex) ||
            !headers.TryGetValue("X-Signature-Timestamp", out var timestamp))
            return false;

        try
        {
            var publicKey = Convert.FromHexString(publicKeyHex);
            var signature = Convert.FromHexString(sigHex);
            var message = Encoding.UTF8.GetBytes(timestamp + Encoding.UTF8.GetString(rawBody));

            // Build SubjectPublicKeyInfo for Ed25519 (OID 1.3.101.112)
            var spkiHeader = new byte[] { 0x30, 0x2a, 0x30, 0x05, 0x06, 0x03, 0x2b, 0x65, 0x70, 0x03, 0x21, 0x00 };
            var spki = new byte[spkiHeader.Length + publicKey.Length];
            Buffer.BlockCopy(spkiHeader, 0, spki, 0, spkiHeader.Length);
            Buffer.BlockCopy(publicKey, 0, spki, spkiHeader.Length, publicKey.Length);

            // TODO: .NET 9 does not have native Ed25519 support in System.Security.Cryptography.
            // ECDsa.Create() cannot import Ed25519 SPKI keys. To get real signature verification,
            // add the NSec.Cryptography NuGet package and use PublicKeyAlgorithm.Ed25519.
            // Until then, this will throw and we fail closed (return false).
            using var ed = ECDsa.Create();
            ed.ImportSubjectPublicKeyInfo(spki, out _);
            return ed.VerifyData(message, signature, HashAlgorithmName.SHA512);
        }
        catch
        {
            // Ed25519 not supported — fail closed, do not accept unverified requests
            return false;
        }
    }

    public ChannelInboundMessage? ParseInbound(JsonElement body)
    {
        var interactionType = body.TryGetProperty("type", out var t) ? t.GetInt32() : 0;

        // PING verification
        if (interactionType == 1)
            return new ChannelInboundMessage("", "", "", IsChallenge: true,
                ChallengeResponse: JsonSerializer.Serialize(new { type = 1 }));

        // APPLICATION_COMMAND (2) or MESSAGE_COMPONENT (3)
        if (interactionType != 2 && interactionType != 3) return null;

        // Extract user ID and display name
        var userId = "";
        string? displayName = null;
        if (body.TryGetProperty("member", out var member) &&
            member.TryGetProperty("user", out var user))
        {
            userId = user.TryGetProperty("id", out var uid) ? uid.GetString() ?? "" : "";
            displayName = user.TryGetProperty("global_name", out var gn) ? gn.GetString()
                : user.TryGetProperty("username", out var un) ? un.GetString() : null;
        }
        else if (body.TryGetProperty("user", out var dmUser))
        {
            userId = dmUser.TryGetProperty("id", out var dmUid) ? dmUid.GetString() ?? "" : "";
            displayName = dmUser.TryGetProperty("global_name", out var gn) ? gn.GetString()
                : dmUser.TryGetProperty("username", out var un) ? un.GetString() : null;
        }

        var channelId = body.TryGetProperty("channel_id", out var cid) ? cid.GetString() ?? "" : "";

        // Group detection: guild_id present = server (group), absent = DM
        var isGroup = body.TryGetProperty("guild_id", out var gid) && gid.ValueKind != JsonValueKind.Null;
        var groupId = isGroup ? gid.GetString() : null;

        // Deduplication: use interaction id
        var messageId = body.TryGetProperty("id", out var mid) ? mid.GetString() : null;

        // Extract text from interaction data
        var text = "";
        if (body.TryGetProperty("data", out var data))
        {
            if (data.TryGetProperty("content", out var content))
                text = content.GetString() ?? "";
            else if (data.TryGetProperty("options", out var options) &&
                     options.ValueKind == JsonValueKind.Array && options.GetArrayLength() > 0)
            {
                // Slash command: concatenate option values
                var parts = new List<string>();
                foreach (var opt in options.EnumerateArray())
                {
                    if (opt.TryGetProperty("value", out var v))
                        parts.Add(v.GetString() ?? "");
                }
                text = string.Join(" ", parts);
            }
            // Also check for resolved message content (message context menu commands)
            else if (data.TryGetProperty("resolved", out var resolved) &&
                     resolved.TryGetProperty("messages", out var messages) &&
                     messages.ValueKind == JsonValueKind.Object)
            {
                foreach (var msg in messages.EnumerateObject())
                {
                    if (msg.Value.TryGetProperty("content", out var msgContent))
                    {
                        text = msgContent.GetString() ?? "";
                        break;
                    }
                }
            }
        }

        if (string.IsNullOrEmpty(text)) return null;

        // Parse attachments from resolved data
        List<ChannelMediaAttachment>? media = null;
        if (body.TryGetProperty("data", out var data2) &&
            data2.TryGetProperty("resolved", out var resolved2) &&
            resolved2.TryGetProperty("attachments", out var attachments) &&
            attachments.ValueKind == JsonValueKind.Object)
        {
            media = new();
            foreach (var att in attachments.EnumerateObject())
            {
                var url = att.Value.TryGetProperty("url", out var u) ? u.GetString() : null;
                var contentType = att.Value.TryGetProperty("content_type", out var ct2) ? ct2.GetString() ?? "" : "";
                var fileName = att.Value.TryGetProperty("filename", out var fn) ? fn.GetString() : null;
                var mediaType = contentType.StartsWith("image/") ? "image"
                    : contentType.StartsWith("video/") ? "video"
                    : contentType.StartsWith("audio/") ? "audio"
                    : "document";
                media.Add(new ChannelMediaAttachment(mediaType, url, contentType, fileName));
            }
        }

        return new ChannelInboundMessage(
            userId, channelId, text,
            MessageId: messageId,
            IsGroupMessage: isGroup,
            GroupId: groupId,
            SenderDisplayName: displayName,
            Media: media);
    }

    public async Task SendReplyAsync(HttpClient client, Dictionary<string, string> config, string channelId, string text, CancellationToken ct = default)
    {
        if (!config.TryGetValue("botToken", out var token) || string.IsNullOrEmpty(token)) return;

        // Discord supports native Markdown, just chunk
        var chunks = PlatformTextChunker.ChunkForPlatform(text, "discord");

        foreach (var chunk in chunks)
        {
            var payload = JsonSerializer.Serialize(new { content = chunk });
            var request = new HttpRequestMessage(HttpMethod.Post,
                $"https://discord.com/api/v10/channels/{channelId}/messages")
            {
                Content = new StringContent(payload, Encoding.UTF8, "application/json"),
            };
            request.Headers.Authorization = new AuthenticationHeaderValue("Bot", token);
            await client.SendAsync(request, ct);
        }
    }
}
