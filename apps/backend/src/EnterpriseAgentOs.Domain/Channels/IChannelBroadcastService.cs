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

    /// <summary>
    /// Start a platform-specific connection process (e.g. WhatsApp QR pairing).
    /// No-op for platforms that don't need it.
    /// </summary>
    Task StartConnectionAsync(Guid connectionId, string channelType, CancellationToken ct = default);

    /// <summary>
    /// Stop/disconnect a platform-specific connection.
    /// No-op for platforms that don't need it.
    /// </summary>
    Task StopConnectionAsync(Guid connectionId, string channelType, CancellationToken ct = default);
}

/// <summary>
/// Application-level channel orchestration: broadcasting, test messages,
/// and any cross-cutting channel logic that doesn't belong in a resolver.
/// </summary>
public interface IChannelService
{
    // ── Broadcasting ──────────────────────────────────────────────────

    Task BroadcastAsync(Guid agentId, string text, CancellationToken ct = default);
    Task SendTestMessageAsync(Guid connectionId, string destination, CancellationToken ct = default);

    // ── Connection lifecycle ─────────────────────────────────────────

    Task<ChannelConnectionRecord> CreateConnectionAsync(string channelType, string displayName, string? configJson, string? defaultChannelId, Guid createdById, CancellationToken ct = default);
    Task<ChannelConnectionRecord> UpdateConnectionAsync(Guid id, string? displayName, bool? enabled, string? configJson, CancellationToken ct = default);
    Task<bool> DeleteConnectionAsync(Guid id, CancellationToken ct = default);

    // ── WhatsApp creds ───────────────────────────────────────────────

    Task SaveWhatsAppCredsAsync(Guid connectionId, string credsJson, CancellationToken ct = default);
    Task<string?> LoadWhatsAppCredsAsync(Guid connectionId, CancellationToken ct = default);
}
