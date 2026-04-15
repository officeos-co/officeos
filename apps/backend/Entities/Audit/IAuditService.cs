using EnterpriseAgentOs.Api.Database.Models;

namespace EnterpriseAgentOs.Api.Entities.Audit;

public interface IAuditService
{
    Task RecordToolCallAsync(Guid agentId, Guid? userId, string skillName, string action,
        string paramsJson, string? resultSummary, long durationMs);
    Task<(List<AgentToolCallRecord> Items, int Total)> GetAuditLogAsync(Guid agentId, int limit, int offset);
}
