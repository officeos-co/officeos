using EnterpriseAgentOs.Api.Database.Models;

namespace EnterpriseAgentOs.Api.Entities.Audit;

public interface IAuditRepository
{
    Task AddAsync(AgentToolCallRecord record, CancellationToken ct = default);
    Task<(List<AgentToolCallRecord> Items, int Total)> GetByAgentAsync(Guid agentId, int limit, int offset, CancellationToken ct = default);
}
