namespace EnterpriseAgentOs.Domain.Interfaces.Agents;

public interface IAgentRepository
{
    Task<IReadOnlyList<AgentRecord>> ListAsync(CancellationToken ct = default);
    Task<AgentRecord?> GetAsync(Guid id, CancellationToken ct = default);
    Task AddAsync(AgentRecord record, CancellationToken ct = default);
    Task UpdateAsync(AgentRecord record, CancellationToken ct = default);
    Task<bool> SoftDeleteAsync(Guid id, CancellationToken ct = default);
    Task UpdateStatusAsync(Guid id, string status, CancellationToken ct = default);
}
