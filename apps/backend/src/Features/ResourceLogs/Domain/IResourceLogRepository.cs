namespace OffceOs.Domain.Features.ResourceLogs;

public sealed record GlobalResourceLogRow(ResourceLogRecord Log, string AgentName);

public enum ResourceLogSort
{
    TimeAscending,
    TimeDescending,
}

public sealed record ResourceLogListOptions
{
    public int? Skip { get; init; }
    public int? Limit { get; init; }
    public Guid? AfterLogId { get; init; }
    public ResourceLogSort Sort { get; init; } = ResourceLogSort.TimeDescending;
}

public interface IResourceLogRepository
{
    IQueryable<ResourceLogRecord> Query(ResourceLogFilter filter);
    Task<List<ResourceLogRecord>> ListAsync(ResourceLogFilter filter, ResourceLogListOptions? options = null, CancellationToken ct = default);
    Task<int> CountAsync(ResourceLogFilter filter, CancellationToken ct = default);
    Task<ResourceLogRecord> AppendAsync(ResourceLogRecord record, CancellationToken ct = default);
    Task AppendPairAsync(ResourceLogRecord toolCall, ResourceLogRecord toolResult, CancellationToken ct = default);
    Task<ResourceLogRecord?> GetByAsync(ResourceLogFilter filter, CancellationToken ct = default);
    Task<ResourceLogRecord> UpsertQueuedWorkAsync(ResourceLogRecord record, CancellationToken ct = default);
    Task<ResourceLogRecord?> ClaimNextQueuedWorkAsync(CancellationToken ct = default);
    Task MarkWorkAsync(Guid workLogId, string status, string? error = null, CancellationToken ct = default);
    Task DeleteByAgentIdsAsync(IReadOnlyList<Guid> agentIds, CancellationToken ct = default);
}
