namespace EnterpriseAgentOs.Api.Entities.AgentLogs;

public interface IAgentLogService
{
    Task<List<AgentLogRecord>> ListForAgentAsync(Guid agentId, DateTime? before, int limit, CancellationToken ct = default);
    Task<GlobalLogsPage> ListGlobalAsync(GlobalLogFiltersInput filters, CancellationToken ct = default);
    Task<AgentLogRecord> AppendAsync(AgentLogRecord record, CancellationToken ct = default);
    Task<AgentLogRecord> SendMessageAsync(Guid agentId, string content, Guid userId, CancellationToken ct = default);
}
