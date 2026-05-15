namespace OffceOs.Application.Features.Observability;

public interface IAgentLogService
{
    Task<AgentLogPage> ListAsync(AgentLogQueryRequest request, CancellationToken ct = default);
    Task<IReadOnlyDictionary<Guid, string?>> GetLastRelevantMessagesAsync(LastRelevantLogQueryRequest request, CancellationToken ct = default);
    Task<AgentLogRecord> AppendAsync(AgentLogRecord record, CancellationToken ct = default);
    Task<AgentLogRecord> QueueWorkAsync(QueueAgentWorkRequest request, CancellationToken ct = default);
    Task<AgentLogRecord?> GetAsync(Guid logId, CancellationToken ct = default);
    Task<AgentLogRecord?> StartWorkAsync(Guid workLogId, CancellationToken ct = default);
    Task<AgentLogRecord?> ClaimNextQueuedWorkAsync(CancellationToken ct = default);
    Task CompleteWorkAsync(Guid workLogId, CancellationToken ct = default);
    Task FailWorkAsync(Guid workLogId, string error, CancellationToken ct = default);
}

public sealed record AgentLogQueryRequest(
    Guid? WorkspaceId = null,
    Guid? AgentId = null,
    IReadOnlyList<Guid>? AgentIds = null,
    Guid? ChannelConnectionId = null,
    string? ResourceKind = null,
    string? ResourceName = null,
    Guid? ResourceId = null,
    string? CorrelationId = null,
    AgentLogType? Type = null,
    IReadOnlyList<AgentLogType>? Types = null,
    string? WorkStatus = null,
    string? WorkPurpose = null,
    Guid? DefinitionId = null,
    string? Severity = null,
    string? Search = null,
    string? AgentName = null,
    DateTime? Before = null,
    DateTime? FromInclusive = null,
    DateTime? ToExclusive = null,
    int Skip = 0,
    int Limit = 100,
    AgentLogSort Sort = AgentLogSort.TimeDescending);

public sealed record AgentLogPage(IReadOnlyList<AgentLogRecord> Items, int Total);

public sealed record QueueAgentWorkRequest(
    Guid AgentId,
    Guid? WorkspaceId,
    string Content,
    string CorrelationId,
    string Purpose,
    Guid? DefinitionId = null,
    DateTime? Time = null);

public sealed record LastRelevantLogQueryRequest(
    IReadOnlyCollection<Guid>? AgentIds = null,
    IReadOnlyCollection<Guid>? ChannelConnectionIds = null,
    Guid? WorkspaceId = null);
