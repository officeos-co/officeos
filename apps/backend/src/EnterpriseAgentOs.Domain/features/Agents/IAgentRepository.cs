namespace EnterpriseAgentOs.Domain.Features.Agents;

public interface IAgentRepository
{
    Task<IReadOnlyList<AgentRecord>> ListAsync(CancellationToken ct = default);
    Task<AgentRecord?> GetAsync(Guid id, CancellationToken ct = default);
    Task AddAsync(AgentRecord record, CancellationToken ct = default);
    Task UpdateAsync(AgentRecord record, CancellationToken ct = default);
    Task<bool> SoftDeleteAsync(Guid id, CancellationToken ct = default);
    Task UpdateStatusAsync(Guid id, string status, CancellationToken ct = default);
    Task<IReadOnlyList<AgentRecord>> ListByOwnerAsync(Guid ownerId, bool includeDeleted = false, CancellationToken ct = default);
    Task HardDeleteByOwnerAsync(Guid ownerId, CancellationToken ct = default);
}
