namespace OffceOs.Application.Features.Analytics;

public interface IAgentLogService
{
    IQueryable<AgentLogProjection> AgentLogs(Guid agentId);
    IQueryable<AgentLogProjection> ChannelLogs(Guid channelConnectionId);
    IQueryable<AgentLogProjection> GlobalLogs(GlobalLogFiltersRequest filters);
    IQueryable<AuditEntry> AuditLog(Guid agentId);
    Task<List<AgentLogRecord>> ListForAgentAsync(Guid agentId, DateTime? before, int limit, CancellationToken ct = default);
    Task<List<AgentLogRecord>> ListForChannelConnectionAsync(Guid channelConnectionId, DateTime? before, int limit, CancellationToken ct = default);
    Task<GlobalLogsPage> ListGlobalAsync(GlobalLogFiltersRequest filters, CancellationToken ct = default);
    Task<AgentLogRecord> AppendAsync(AgentLogRecord record, CancellationToken ct = default);
    Task<AgentLogRecord> SendMessageAsync(Guid agentId, string content, Guid userId, CancellationToken ct = default);
    Task RecordToolCallAsync(Guid agentId, Guid? userId, string skillName, string action,
        string paramsJson, string? resultSummary, long durationMs, CancellationToken ct = default);
    Task<(List<AgentLogRecord> Items, int Total)> GetAuditLogAsync(Guid agentId, int limit, int offset, CancellationToken ct = default);
    Task<Dictionary<string, AgentLogRecord>> GetResultsByCorrelationAsync(Guid agentId, IReadOnlyCollection<string> correlationIds, CancellationToken ct = default);
}

public interface IUsageAnalyticsService
{
    Task<UsageAnalyticsResult> GetForUserAsync(Guid userId, UsageAnalyticsRequest input, CancellationToken ct = default);
}
