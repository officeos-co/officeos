namespace EnterpriseAgentOs.Domain.Features.Channels;

/// <summary>
/// Proxy to the channel microservice. All platform logic lives there.
/// </summary>
public interface IChannelGateway
{
    Task SendAsync(Guid connectionId, string text, CancellationToken ct = default);
    Task StartConnectionAsync(Guid connectionId, string channelType, CancellationToken ct = default);
    Task StopConnectionAsync(Guid connectionId, string channelType, CancellationToken ct = default);
    Task SaveCredsAsync(Guid connectionId, string credsJson, CancellationToken ct = default);
}

/// <summary>
/// Application-level channel orchestration. Backend owns bindings + metadata,
/// delegates all platform work to the channel microservice via IChannelGateway.
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
