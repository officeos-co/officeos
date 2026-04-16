namespace EnterpriseAgentOs.Api.Entities.Audit;

public interface IAuditRepository
{
    Task AddPairAsync(EnterpriseAgentOs.Api.Database.Models.AgentLogRecord toolCall, EnterpriseAgentOs.Api.Database.Models.AgentLogRecord toolResult, CancellationToken ct = default);
    Task<(List<EnterpriseAgentOs.Api.Database.Models.AgentLogRecord> Items, int Total)> GetByAgentAsync(Guid agentId, int limit, int offset, CancellationToken ct = default);
}
