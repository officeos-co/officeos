namespace OffceOs.Application.Features.Observability;

public interface IAgentLogService
{
    Task<AgentLogPage> ListAsync(AgentLogQueryRequest request, CancellationToken ct = default);
    Task<IReadOnlyDictionary<Guid, string?>> GetLastRelevantMessagesAsync(LastRelevantLogQueryRequest request, CancellationToken ct = default);
    Task<AgentLogRecord> AppendAsync(AgentLogRecord record, CancellationToken ct = default);
}

public sealed record AgentLogQueryRequest(
    Guid? WorkspaceId = null,
    Guid? AgentId = null,
    IReadOnlyList<Guid>? AgentIds = null,
    Guid? RunId = null,
    Guid? ChannelConnectionId = null,
    string? ResourceKind = null,
    string? ResourceName = null,
    Guid? ResourceId = null,
    AgentLogType? Type = null,
    IReadOnlyList<AgentLogType>? Types = null,
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

public sealed record LastRelevantLogQueryRequest(
    IReadOnlyCollection<Guid>? AgentIds = null,
    IReadOnlyCollection<Guid>? ChannelConnectionIds = null,
    Guid? WorkspaceId = null);
