namespace EnterpriseAgentOs.Api.Entities.Audit;

public interface IAuditRepository
{
    Task AddPairAsync(AgentLogRecord toolCall, AgentLogRecord toolResult, CancellationToken ct = default);
    Task<(List<AgentLogRecord> Items, int Total)> GetByAgentAsync(Guid agentId, int limit, int offset, CancellationToken ct = default);
}
