namespace EnterpriseAgentOs.Domain.Features.Agents;

public sealed record AgentSessionFilter
{
    public Guid? Id { get; init; }
    public Guid? AgentId { get; init; }
    public SessionStatus? Status { get; init; }
}

public interface IAgentSessionRepository
{
    Task<AgentSessionRecord?> GetByAsync(AgentSessionFilter filter, CancellationToken ct = default);
    Task<IReadOnlyList<AgentSessionRecord>> ListByAgentAsync(Guid agentId, int limit = 20, CancellationToken ct = default);
    Task<AgentSessionRecord> CreateAsync(AgentSessionRecord session, CancellationToken ct = default);
    Task SaveChangesAsync(CancellationToken ct = default);
    Task<int> CountByAgentAsync(Guid agentId, CancellationToken ct = default);
}
