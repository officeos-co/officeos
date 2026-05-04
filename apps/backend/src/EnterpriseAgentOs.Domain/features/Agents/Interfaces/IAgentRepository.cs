namespace EnterpriseAgentOs.Domain.Features.Agents;

public sealed record AgentFilter
{
    public Guid? Id { get; init; }
    public Guid? OwnerId { get; init; }
    public bool IncludeDeleted { get; init; }
}

public interface IAgentRepository
{
    Task<IReadOnlyList<AgentRecord>> ListAsync(CancellationToken ct = default);

    /// <summary>
    /// Loads the full agent aggregate — personality files, installed skills,
    /// skill details, memories, and active session.
    /// </summary>
    Task<AgentRecord?> GetByAsync(AgentFilter filter, CancellationToken ct = default);

    Task AddAsync(AgentRecord record, CancellationToken ct = default);
    Task UpdateAsync(AgentRecord record, CancellationToken ct = default);
    Task<bool> SoftDeleteAsync(Guid id, CancellationToken ct = default);
    Task UpdateStatusAsync(Guid id, AgentStatus status, CancellationToken ct = default);
    Task<IReadOnlyList<AgentRecord>> ListByOwnerAsync(Guid ownerId, bool includeDeleted = false, CancellationToken ct = default);
    Task HardDeleteByOwnerAsync(Guid ownerId, CancellationToken ct = default);
}
