namespace OffceOs.Features.Agents.Domain;

public interface IAgentSessionRepository
{
    Task<AgentSessionRecord?> GetByAsync(AgentSessionFilter filter, CancellationToken ct = default);
    Task<IReadOnlyList<AgentSessionRecord>> ListByAgentAsync(Guid agentId, int limit = 20, CancellationToken ct = default);
    Task<AgentSessionRecord> CreateAsync(AgentSessionRecord session, CancellationToken ct = default);
    Task SaveChangesAsync(CancellationToken ct = default);
    Task<int> CountByAgentAsync(Guid agentId, CancellationToken ct = default);
}
