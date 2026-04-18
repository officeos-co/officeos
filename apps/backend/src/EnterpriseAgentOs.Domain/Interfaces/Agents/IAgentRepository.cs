namespace EnterpriseAgentOs.Domain.Interfaces.Agents;

public interface IAgentRepository
{
    Task<IReadOnlyList<EnterpriseAgentOs.Domain.Models.AgentRecord>> ListAsync(CancellationToken ct = default);
    Task<EnterpriseAgentOs.Domain.Models.AgentRecord?> GetAsync(Guid id, CancellationToken ct = default);
    Task AddAsync(EnterpriseAgentOs.Domain.Models.AgentRecord record, CancellationToken ct = default);
    Task UpdateAsync(EnterpriseAgentOs.Domain.Models.AgentRecord record, CancellationToken ct = default);
    Task<bool> SoftDeleteAsync(Guid id, CancellationToken ct = default);
    Task UpdateStatusAsync(Guid id, string status, CancellationToken ct = default);
}
