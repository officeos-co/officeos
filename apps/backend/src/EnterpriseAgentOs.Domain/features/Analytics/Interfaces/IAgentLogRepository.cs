namespace EnterpriseAgentOs.Domain.Features.Analytics;

public sealed record GlobalLogRow(AgentLogRecord Log, string AgentName);

public interface IAgentLogRepository
{
    Task<List<AgentLogRecord>> ListAsync(Guid agentId, DateTime? before, int limit, CancellationToken ct = default);
    Task<List<AgentLogRecord>> ListAfterAsync(Guid agentId, Guid? afterLogId, int limit, CancellationToken ct = default);
    Task<(List<GlobalLogRow> Items, int Total)> ListGlobalAsync(
        string? search, string? agentName, AgentLogType? type, int skip, int limit, CancellationToken ct = default);
    Task<AgentLogRecord> AppendAsync(AgentLogRecord record, CancellationToken ct = default);
    Task AppendPairAsync(AgentLogRecord toolCall, AgentLogRecord toolResult, CancellationToken ct = default);
    Task<AgentLogRecord?> GetByIdAsync(Guid id, CancellationToken ct = default);
    Task<(List<AgentLogRecord> Items, int Total)> GetToolCallsAsync(Guid agentId, int limit, int offset, CancellationToken ct = default);
    Task<Dictionary<string, AgentLogRecord>> GetResultsByCorrelationAsync(Guid agentId, IReadOnlyCollection<string> correlationIds, CancellationToken ct = default);
    Task DeleteByAgentIdsAsync(IReadOnlyList<Guid> agentIds, CancellationToken ct = default);
    Task<List<AgentLogRecord>> ListByAgentIdsAsync(IReadOnlyList<Guid> agentIds, IReadOnlyList<AgentLogType>? types = null, CancellationToken ct = default);
}
