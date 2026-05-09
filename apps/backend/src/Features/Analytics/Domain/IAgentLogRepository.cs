namespace EnterpriseAgentOs.Domain.Features.Analytics;

public sealed record GlobalLogRow(AgentLogRecord Log, string AgentName);

public sealed record UsageAggregateRow(
    DateTime Date,
    string Model,
    long InputTokens,
    long OutputTokens);

public enum AgentLogSort
{
    TimeAscending,
    TimeDescending,
}

public sealed record AgentLogListOptions
{
    public int? Skip { get; init; }
    public int? Limit { get; init; }
    public Guid? AfterLogId { get; init; }
    public AgentLogSort Sort { get; init; } = AgentLogSort.TimeDescending;
}

public interface IAgentLogRepository
{
    IQueryable<AgentLogRecord> Query(AgentLogFilter filter);
    Task<List<AgentLogRecord>> ListAsync(AgentLogFilter filter, AgentLogListOptions? options = null, CancellationToken ct = default);
    Task<List<UsageAggregateRow>> ListUsageAggregatesAsync(Guid ownerId, DateTime fromInclusive, DateTime toExclusive, CancellationToken ct = default);
    Task<int> CountAsync(AgentLogFilter filter, CancellationToken ct = default);
    Task<AgentLogRecord> AppendAsync(AgentLogRecord record, CancellationToken ct = default);
    Task AppendPairAsync(AgentLogRecord toolCall, AgentLogRecord toolResult, CancellationToken ct = default);
    Task<AgentLogRecord?> GetByAsync(AgentLogFilter filter, CancellationToken ct = default);
    Task DeleteByAgentIdsAsync(IReadOnlyList<Guid> agentIds, CancellationToken ct = default);
}
