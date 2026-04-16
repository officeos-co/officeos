namespace EnterpriseAgentOs.Api.Entities.AgentLogs;

public sealed record GlobalLogRow(AgentLogRecord Log, string AgentName);

public interface IAgentLogRepository
{
    Task<List<AgentLogRecord>> ListAsync(Guid agentId, DateTime? before, int limit, CancellationToken ct = default);
    Task<(List<GlobalLogRow> Items, int Total)> ListGlobalAsync(
        string? search, string? agentName, AgentLogType? type, int skip, int limit, CancellationToken ct = default);
    Task<AgentLogRecord> AppendAsync(AgentLogRecord record, CancellationToken ct = default);
    Task<AgentLogRecord?> GetByIdAsync(Guid id, CancellationToken ct = default);
}
