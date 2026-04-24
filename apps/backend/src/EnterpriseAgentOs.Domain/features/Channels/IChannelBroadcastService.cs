namespace EnterpriseAgentOs.Domain.Features.Channels;

/// <summary>
/// Proxy to the channel sidecar. Runs in the same K8s pod on localhost.
/// </summary>
public interface IChannelGateway
{
    Task SendAsync(string channelType, string platformId, string? threadId,
                   object message, CancellationToken ct = default);
    Task ReloadAsync(CancellationToken ct = default);
}

/// <summary>
/// Application-level channel orchestration. Backend owns bindings + metadata,
/// delegates platform delivery to the channel sidecar via IChannelGateway.
/// </summary>
public interface IChannelService
{
    Task BroadcastAsync(Guid agentId, string text, CancellationToken ct = default);
    Task SendTestMessageAsync(Guid connectionId, CancellationToken ct = default);
    Task<ChannelConnectionRecord> CreateConnectionAsync(string channelType, string displayName, string? configJson, Guid createdById, CancellationToken ct = default);
    Task<ChannelConnectionRecord> UpdateConnectionAsync(Guid id, string? displayName, bool? enabled, CancellationToken ct = default);
    Task<bool> DeleteConnectionAsync(Guid id, CancellationToken ct = default);
    Task SaveChannelCredsAsync(Guid connectionId, string credsJson, CancellationToken ct = default);
}
