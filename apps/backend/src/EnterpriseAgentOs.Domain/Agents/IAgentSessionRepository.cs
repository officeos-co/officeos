namespace EnterpriseAgentOs.Domain.Agents;

public interface IAgentSessionRepository
{
    Task<AgentSessionRecord?> GetActiveAsync(Guid agentId, CancellationToken ct = default);
    Task<AgentSessionRecord?> GetByIdAsync(Guid sessionId, CancellationToken ct = default);
    Task<IReadOnlyList<AgentSessionRecord>> ListByAgentAsync(Guid agentId, int limit = 20, CancellationToken ct = default);
    Task<AgentSessionRecord> CreateAsync(AgentSessionRecord session, CancellationToken ct = default);
    Task SaveChangesAsync(CancellationToken ct = default);
    Task<int> CountByAgentAsync(Guid agentId, CancellationToken ct = default);
}
