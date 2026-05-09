namespace EnterpriseAgentOs.Domain.Features.Agents;

public sealed record AgentFilter
{
    public Guid? Id { get; init; }
    public Guid? OwnerId { get; init; }
    public bool IncludeDeleted { get; init; }
}

public interface IAgentRepository
{
    Task<IReadOnlyList<AgentRecord>> ListAsync(AgentFilter filter, CancellationToken ct = default);

    /// <summary>
    /// Loads the full agent aggregate — personality files, installed skills,
    /// skill details, memories, and active session.
    /// </summary>
    Task<AgentRecord?> GetByAsync(AgentFilter filter, CancellationToken ct = default);

    Task AddAsync(AgentRecord record, CancellationToken ct = default);
    Task UpdateAsync(AgentRecord record, CancellationToken ct = default);
    Task<bool> SoftDeleteAsync(AgentFilter filter, CancellationToken ct = default);
    Task UpdateStatusAsync(AgentFilter filter, AgentStatus status, CancellationToken ct = default);
    Task HardDeleteAsync(AgentFilter filter, CancellationToken ct = default);
}
