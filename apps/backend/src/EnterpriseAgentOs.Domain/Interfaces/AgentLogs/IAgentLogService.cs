namespace EnterpriseAgentOs.Domain.Interfaces.AgentLogs;

public interface IAgentLogService
{
    Task<List<EnterpriseAgentOs.Domain.Models.AgentLogRecord>> ListForAgentAsync(Guid agentId, DateTime? before, int limit, CancellationToken ct = default);
    Task<GlobalLogsPage> ListGlobalAsync(GlobalLogFiltersInput filters, CancellationToken ct = default);
    Task<EnterpriseAgentOs.Domain.Models.AgentLogRecord> AppendAsync(EnterpriseAgentOs.Domain.Models.AgentLogRecord record, CancellationToken ct = default);
    Task<EnterpriseAgentOs.Domain.Models.AgentLogRecord> SendMessageAsync(Guid agentId, string content, Guid userId, CancellationToken ct = default);

    // Audit (merged from Entities/Audit)
    Task RecordToolCallAsync(Guid agentId, Guid? userId, string skillName, string action,
        string paramsJson, string? resultSummary, long durationMs, CancellationToken ct = default);
    Task<(List<EnterpriseAgentOs.Domain.Models.AgentLogRecord> Items, int Total)> GetAuditLogAsync(Guid agentId, int limit, int offset, CancellationToken ct = default);
    Task<Dictionary<string, EnterpriseAgentOs.Domain.Models.AgentLogRecord>> GetResultsByCorrelationAsync(Guid agentId, IReadOnlyCollection<string> correlationIds, CancellationToken ct = default);
}
