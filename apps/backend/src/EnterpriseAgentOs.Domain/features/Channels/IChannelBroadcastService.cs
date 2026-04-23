namespace EnterpriseAgentOs.Domain.Features.Channels;

/// <summary>
/// Infrastructure abstraction: sends a single message through a channel
/// connection. The gateway resolves the destination internally per platform.
/// </summary>
public interface IChannelGateway
{
    /// <summary>
    /// Send <paramref name="text"/> through the channel connection identified
    /// by <paramref name="connectionId"/>. The gateway resolves the destination
    /// internally from the connection's credentials/config.
    /// </summary>
    Task SendAsync(Guid connectionId, string text, CancellationToken ct = default);

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
    Task SendTestMessageAsync(Guid connectionId, CancellationToken ct = default);

    // ── Connection lifecycle ─────────────────────────────────────────

    Task<ChannelConnectionRecord> CreateConnectionAsync(string channelType, string displayName, string? configJson, string? defaultChannelId, Guid createdById, CancellationToken ct = default);
    Task<ChannelConnectionRecord> UpdateConnectionAsync(Guid id, string? displayName, bool? enabled, string? configJson, CancellationToken ct = default);
    Task<bool> DeleteConnectionAsync(Guid id, CancellationToken ct = default);

    // ── Channel creds ───────────────────────────────────────────────

    Task SaveChannelCredsAsync(Guid connectionId, string credsJson, CancellationToken ct = default);
    Task<string?> LoadChannelCredsAsync(Guid connectionId, CancellationToken ct = default);
}
