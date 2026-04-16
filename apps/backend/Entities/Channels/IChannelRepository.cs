namespace EnterpriseAgentOs.Api.Entities.Channels;

public interface IChannelRepository
{
    // ---------- Channel Connections ----------
    Task<IReadOnlyList<EnterpriseAgentOs.Api.Database.Models.ChannelConnectionRecord>> ListConnectionsAsync(CancellationToken ct = default);
    Task<EnterpriseAgentOs.Api.Database.Models.ChannelConnectionRecord?> GetConnectionAsync(Guid id, CancellationToken ct = default);
    Task<EnterpriseAgentOs.Api.Database.Models.ChannelConnectionRecord> CreateConnectionAsync(EnterpriseAgentOs.Api.Database.Models.ChannelConnectionRecord record, CancellationToken ct = default);
    Task<EnterpriseAgentOs.Api.Database.Models.ChannelConnectionRecord?> UpdateConnectionAsync(Guid id, Action<EnterpriseAgentOs.Api.Database.Models.ChannelConnectionRecord> apply, CancellationToken ct = default);
    Task<bool> DeleteConnectionAsync(Guid id, CancellationToken ct = default);

    // ---------- Agent Channel Bindings ----------
    Task<IReadOnlyList<EnterpriseAgentOs.Api.Database.Models.AgentChannelBindingRecord>> ListBindingsAsync(Guid agentId, CancellationToken ct = default);
    Task<EnterpriseAgentOs.Api.Database.Models.AgentChannelBindingRecord?> GetBindingAsync(Guid bindingId, CancellationToken ct = default);
    Task<EnterpriseAgentOs.Api.Database.Models.AgentChannelBindingRecord> CreateBindingAsync(EnterpriseAgentOs.Api.Database.Models.AgentChannelBindingRecord record, CancellationToken ct = default);
    Task<EnterpriseAgentOs.Api.Database.Models.AgentChannelBindingRecord?> UpdateBindingAsync(Guid bindingId, Action<EnterpriseAgentOs.Api.Database.Models.AgentChannelBindingRecord> apply, CancellationToken ct = default);
    Task<bool> DeleteBindingAsync(Guid bindingId, CancellationToken ct = default);

    // ---------- Routing queries ----------
    Task<IReadOnlyList<EnterpriseAgentOs.Api.Database.Models.AgentChannelBindingRecord>> FindBindingsByConnectionAsync(Guid connectionId, CancellationToken ct = default);
}
