namespace EnterpriseAgentOs.Api.Entities.AgentLogs;

public interface IAgentLogService
{
    Task<List<EnterpriseAgentOs.Api.Database.Models.AgentLogRecord>> ListForAgentAsync(Guid agentId, DateTime? before, int limit, CancellationToken ct = default);
    Task<GlobalLogsPage> ListGlobalAsync(GlobalLogFiltersInput filters, CancellationToken ct = default);
    Task<EnterpriseAgentOs.Api.Database.Models.AgentLogRecord> AppendAsync(EnterpriseAgentOs.Api.Database.Models.AgentLogRecord record, CancellationToken ct = default);
    Task<EnterpriseAgentOs.Api.Database.Models.AgentLogRecord> SendMessageAsync(Guid agentId, string content, Guid userId, CancellationToken ct = default);
}
