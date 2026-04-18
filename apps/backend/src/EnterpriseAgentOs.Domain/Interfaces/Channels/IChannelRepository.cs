namespace EnterpriseAgentOs.Domain.Interfaces.Channels;

public interface IChannelRepository
{
    // ---------- Channel Connections ----------
    Task<IReadOnlyList<EnterpriseAgentOs.Domain.Models.ChannelConnectionRecord>> ListConnectionsAsync(CancellationToken ct = default);
    Task<EnterpriseAgentOs.Domain.Models.ChannelConnectionRecord?> GetConnectionAsync(Guid id, CancellationToken ct = default);
    Task<EnterpriseAgentOs.Domain.Models.ChannelConnectionRecord> CreateConnectionAsync(EnterpriseAgentOs.Domain.Models.ChannelConnectionRecord record, CancellationToken ct = default);
    Task<EnterpriseAgentOs.Domain.Models.ChannelConnectionRecord?> UpdateConnectionAsync(Guid id, Action<EnterpriseAgentOs.Domain.Models.ChannelConnectionRecord> apply, CancellationToken ct = default);
    Task<bool> DeleteConnectionAsync(Guid id, CancellationToken ct = default);

    // ---------- Agent Channel Bindings ----------
    Task<IReadOnlyList<EnterpriseAgentOs.Domain.Models.AgentChannelBindingRecord>> ListBindingsAsync(Guid agentId, CancellationToken ct = default);
    Task<EnterpriseAgentOs.Domain.Models.AgentChannelBindingRecord?> GetBindingAsync(Guid bindingId, CancellationToken ct = default);
    Task<EnterpriseAgentOs.Domain.Models.AgentChannelBindingRecord> CreateBindingAsync(EnterpriseAgentOs.Domain.Models.AgentChannelBindingRecord record, CancellationToken ct = default);
    Task<EnterpriseAgentOs.Domain.Models.AgentChannelBindingRecord?> UpdateBindingAsync(Guid bindingId, Action<EnterpriseAgentOs.Domain.Models.AgentChannelBindingRecord> apply, CancellationToken ct = default);
    Task<bool> DeleteBindingAsync(Guid bindingId, CancellationToken ct = default);

    // ---------- Routing queries ----------
    Task<IReadOnlyList<EnterpriseAgentOs.Domain.Models.AgentChannelBindingRecord>> FindBindingsByConnectionAsync(Guid connectionId, CancellationToken ct = default);
}
