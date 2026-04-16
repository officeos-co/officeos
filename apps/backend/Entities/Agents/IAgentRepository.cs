namespace EnterpriseAgentOs.Api.Entities.Agents;

public interface IAgentRepository
{
    Task<IReadOnlyList<EnterpriseAgentOs.Api.Database.Models.AgentRecord>> ListAsync(CancellationToken ct = default);
    Task<EnterpriseAgentOs.Api.Database.Models.AgentRecord?> GetAsync(Guid id, CancellationToken ct = default);
    Task AddAsync(EnterpriseAgentOs.Api.Database.Models.AgentRecord record, CancellationToken ct = default);
    Task UpdateAsync(EnterpriseAgentOs.Api.Database.Models.AgentRecord record, CancellationToken ct = default);
    Task<bool> SoftDeleteAsync(Guid id, CancellationToken ct = default);
    Task UpdateStatusAsync(Guid id, string status, CancellationToken ct = default);
}
