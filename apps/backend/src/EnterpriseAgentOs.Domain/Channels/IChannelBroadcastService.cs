namespace EnterpriseAgentOs.Domain.Channels;

/// <summary>
/// Infrastructure abstraction: sends a single message to a specific channel
/// connection + destination. Platform-specific details (WhatsApp JID vs
/// Slack channel ID vs adapter.SendReplyAsync) live behind this interface.
/// </summary>
public interface IChannelGateway
{
    /// <summary>
    /// Send <paramref name="text"/> through the channel connection identified
    /// by <paramref name="connectionId"/> to the given <paramref name="destination"/>
    /// (JID, Slack channel ID, etc.).
    /// </summary>
    Task SendAsync(Guid connectionId, string channelType, string destination, string text, CancellationToken ct = default);
}

/// <summary>
/// Application-level channel orchestration: broadcasting, test messages,
/// and any cross-cutting channel logic that doesn't belong in a resolver.
/// </summary>
public interface IChannelService
{
    /// <summary>
    /// Broadcast a text message to every channel bound to the given agent.
    /// Skips bindings with no known reply-to destination.
    /// </summary>
    Task BroadcastAsync(Guid agentId, string text, CancellationToken ct = default);

    /// <summary>
    /// Send a test/welcome message through a connection to confirm it is working.
    /// Called when a channel is first connected (QR scan, webhook config, etc.).
    /// </summary>
    Task SendTestMessageAsync(Guid connectionId, string destination, CancellationToken ct = default);

    /// <summary>
    /// Persist WhatsApp session credentials. On first pairing (no prior config),
    /// extracts the owner JID and sends a test message automatically.
    /// </summary>
    Task SaveWhatsAppCredsAsync(Guid connectionId, string credsJson, CancellationToken ct = default);

    /// <summary>
    /// Load WhatsApp session credentials for the sidecar.
    /// Returns the raw creds JSON, or null if not found.
    /// </summary>
    Task<string?> LoadWhatsAppCredsAsync(Guid connectionId, CancellationToken ct = default);
}
