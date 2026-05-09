namespace EnterpriseAgentOs.Domain.Features.Channels;

public interface IChannelRepository
{
    // ---------- Channel Connections ----------
    Task<IReadOnlyList<ChannelConnectionRecord>> ListConnectionsAsync(ChannelConnectionFilter? filter = null, CancellationToken ct = default);
    Task<ChannelConnectionRecord?> GetConnectionByAsync(ChannelConnectionFilter filter, CancellationToken ct = default);
    Task<ChannelConnectionRecord> CreateConnectionAsync(ChannelConnectionRecord record, CancellationToken ct = default);
    Task<ChannelConnectionRecord?> UpdateConnectionAsync(Guid id, Action<ChannelConnectionRecord> apply, CancellationToken ct = default);
    Task<bool> DeleteConnectionAsync(Guid id, CancellationToken ct = default);

    // ---------- Agent Channel Bindings ----------
    Task<IReadOnlyList<AgentChannelBindingRecord>> ListBindingsAsync(Guid agentId, CancellationToken ct = default);
    Task<AgentChannelBindingRecord?> GetBindingByAsync(AgentChannelBindingFilter filter, CancellationToken ct = default);
    Task<AgentChannelBindingRecord> CreateBindingAsync(AgentChannelBindingRecord record, CancellationToken ct = default);
    Task<AgentChannelBindingRecord?> UpdateBindingAsync(Guid bindingId, Action<AgentChannelBindingRecord> apply, CancellationToken ct = default);
    Task<bool> DeleteBindingAsync(Guid bindingId, CancellationToken ct = default);

    // ---------- Routing queries ----------
    Task<IReadOnlyList<AgentChannelBindingRecord>> FindBindingsByConnectionAsync(Guid connectionId, CancellationToken ct = default);
    Task<IReadOnlyList<ChannelConnectionRecord>> FindConnectionsByChannelTypeAsync(string channelType, CancellationToken ct = default);
}
