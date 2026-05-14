namespace OffceOs.Application.Features.Observability;

public sealed record AgentLogProjection(
    Guid Id,
    Guid? AgentId,
    string? AgentName,
    DateTime Time,
    AgentLogType Type,
    string? Tool,
    string? Integration,
    string? Channel,
    Guid? ChannelConnectionId,
    string Content,
    int? DurationMs,
    int? InputTokens,
    int? OutputTokens,
    string? CorrelationId);

public sealed record GlobalLogFiltersRequest(
    string? Search = null,
    string? AgentName = null,
    AgentLogType? Type = null,
    int Skip = 0,
    int Limit = 50,
    Guid? WorkspaceId = null);

public sealed record GlobalLogsPage(IReadOnlyList<AgentLogProjection> Items, int Total);

public static class AgentLogMapper
{
    public static AgentLogProjection ToProjection(this AgentLogRecord r, string? agentName = null) => new(
        r.Id, r.AgentId, agentName, r.Time, r.Type,
        r.Tool, r.Integration, r.Channel, r.ChannelConnectionId, r.Content,
        r.Usage.DurationMs, r.Usage.InputTokens, r.Usage.OutputTokens, r.CorrelationId);
}
