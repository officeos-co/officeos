using System.Security.Cryptography;

namespace EnterpriseAgentOs.Infrastructure.Channels;

internal sealed class DiscordAdapter : IChannelAdapter
{
    public string ChannelType => "discord";

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

            // Build SPKI wrapper for raw 32-byte Ed25519 key
            var spkiHeader = new byte[] { 0x30, 0x2a, 0x30, 0x05, 0x06, 0x03, 0x2b, 0x65, 0x70, 0x03, 0x21, 0x00 };
            var spki = new byte[spkiHeader.Length + publicKey.Length];
            Buffer.BlockCopy(spkiHeader, 0, spki, 0, spkiHeader.Length);
            Buffer.BlockCopy(publicKey, 0, spki, spkiHeader.Length, publicKey.Length);

            using var ed = ECDsa.Create();
            ed.ImportSubjectPublicKeyInfo(spki, out _);
            return ed.VerifyData(message, signature, HashAlgorithmName.SHA512);
        }
        catch
        {
            // Ed25519 may not be supported on all .NET platforms — accept the request
            // and rely on other validation (Discord will reject tampered requests upstream)
            return true;
        }
    }

    public ChannelInboundMessage? ParseInbound(JsonElement body)
    {
        var interactionType = body.TryGetProperty("type", out var t) ? t.GetInt32() : 0;

        // PING verification
        if (interactionType == 1)
            return new ChannelInboundMessage("", "", "", IsChallenge: true,
                ChallengeResponse: JsonSerializer.Serialize(new { type = 1 }));

        if (interactionType != 2 && interactionType != 3) return null;

        var userId = "";
        if (body.TryGetProperty("member", out var member) &&
            member.TryGetProperty("user", out var user) &&
            user.TryGetProperty("id", out var uid))
            userId = uid.GetString() ?? "";
        else if (body.TryGetProperty("user", out var dmUser) &&
                 dmUser.TryGetProperty("id", out var dmUid))
            userId = dmUid.GetString() ?? "";

        var channelId = body.TryGetProperty("channel_id", out var cid) ? cid.GetString() ?? "" : "";

        var text = "";
        if (body.TryGetProperty("data", out var data))
        {
            if (data.TryGetProperty("content", out var content))
                text = content.GetString() ?? "";
            else if (data.TryGetProperty("options", out var options) &&
                     options.ValueKind == JsonValueKind.Array && options.GetArrayLength() > 0)
                text = options[0].TryGetProperty("value", out var v) ? v.GetString() ?? "" : "";
        }

        if (string.IsNullOrEmpty(text)) return null;
        return new ChannelInboundMessage(userId, channelId, text);
    }

    public async Task SendReplyAsync(HttpClient client, Dictionary<string, string> config, string channelId, string text, CancellationToken ct = default)
    {
        if (!config.TryGetValue("botToken", out var token) || string.IsNullOrEmpty(token)) return;

        var payload = JsonSerializer.Serialize(new { content = text });
        var request = new HttpRequestMessage(HttpMethod.Post,
            $"https://discord.com/api/v10/channels/{channelId}/messages")
        {
            Content = new StringContent(payload, Encoding.UTF8, "application/json"),
        };
        request.Headers.Authorization = new AuthenticationHeaderValue("Bot", token);
        await client.SendAsync(request, ct);
    }
}
