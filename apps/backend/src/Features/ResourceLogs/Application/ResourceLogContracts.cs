namespace OffceOs.Application.Features.ResourceLogs;

public interface IResourceLogService
{
    Task<ResourceLogPage> ListAsync(ResourceLogQueryRequest request, CancellationToken ct = default);
    Task<IReadOnlyDictionary<Guid, string?>> GetLastRelevantMessagesAsync(LastRelevantLogQueryRequest request, CancellationToken ct = default);
    Task<ResourceLogRecord> AppendAsync(ResourceLogRecord record, CancellationToken ct = default);
    Task<ResourceLogRecord?> GetAsync(Guid logId, CancellationToken ct = default);
}

public sealed record ResourceLogQueryRequest(
    Guid? WorkspaceId = null,
    Guid? AgentId = null,
    IReadOnlyList<Guid>? AgentIds = null,
    Guid? ChannelConnectionId = null,
    string? ResourceKind = null,
    string? ResourceName = null,
    Guid? ResourceId = null,
    string? CorrelationId = null,
    ResourceLogType? Type = null,
    IReadOnlyList<ResourceLogType>? Types = null,
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
    ResourceLogSort Sort = ResourceLogSort.TimeDescending);

public sealed record ResourceLogPage(IReadOnlyList<ResourceLogRecord> Items, int Total);

public sealed record LastRelevantLogQueryRequest(
    IReadOnlyCollection<Guid>? AgentIds = null,
    IReadOnlyCollection<Guid>? ChannelConnectionIds = null,
    Guid? WorkspaceId = null);
