namespace EnterpriseAgentOs.Infrastructure.Adapters.Channels;

/// <summary>
/// Normalized inbound message from any channel platform.
/// </summary>
public sealed record ChannelInboundMessage(
    string SenderIdentifier,
    string ChannelId,
    string Text,
    bool IsChallenge = false,
    string? ChallengeResponse = null);

/// <summary>
/// Platform-specific adapter for receiving, verifying, and sending channel messages.
/// Each supported platform implements this interface.
/// </summary>
public interface IChannelAdapter
{
    /// <summary>The channel type identifier (e.g. "slack", "telegram").</summary>
    string ChannelType { get; }

    /// <summary>
    /// Verify the incoming webhook request signature using platform-specific mechanisms.
    /// </summary>
    bool VerifySignature(
        byte[] rawBody,
        Dictionary<string, string> config,
        IDictionary<string, string> headers);

    /// <summary>
    /// Parse the raw webhook body into a normalized inbound message.
    /// Returns null if the request should be ignored.
    /// </summary>
    ChannelInboundMessage? ParseInbound(JsonElement body);

    /// <summary>
    /// Send a reply back through the platform API.
    /// </summary>
    Task SendReplyAsync(
        HttpClient client,
        Dictionary<string, string> config,
        string channelId,
        string text,
        CancellationToken ct = default);
}
