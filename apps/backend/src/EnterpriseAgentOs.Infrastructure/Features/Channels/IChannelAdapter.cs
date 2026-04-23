namespace EnterpriseAgentOs.Infrastructure.Features.Channels;

/// <summary>
/// Normalized inbound message from any channel platform.
/// </summary>
public sealed record ChannelInboundMessage(
    string SenderIdentifier,
    string ChannelId,
    string Text,
    bool IsChallenge = false,
    string? ChallengeResponse = null,
    string? MessageId = null,
    bool IsGroupMessage = false,
    string? GroupId = null,
    string? SenderDisplayName = null,
    string? ParticipantJid = null,
    List<ChannelMediaAttachment>? Media = null);

public sealed record ChannelMediaAttachment(
    string Type,
    string? Url,
    string? MimeType,
    string? FileName);

/// <summary>
/// Platform-specific adapter for receiving, verifying, and sending channel messages.
/// </summary>
public interface IChannelAdapter
{
    string ChannelType { get; }
    int MaxMessageLength => 4000;
    bool VerifySignature(byte[] rawBody, Dictionary<string, string> config, IDictionary<string, string> headers);
    ChannelInboundMessage? ParseInbound(JsonElement body);
    Task SendReplyAsync(HttpClient client, Dictionary<string, string> config, string channelId, string text, CancellationToken ct = default);
}
