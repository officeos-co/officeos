namespace EnterpriseAgentOs.Domain.Features.Analytics;

public sealed record AgentLogDto(
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

public sealed record GlobalLogFiltersInput(
    string? Search = null,
    string? AgentName = null,
    AgentLogType? Type = null,
    int Skip = 0,
    int Limit = 50);

public sealed record GlobalLogsPage(IReadOnlyList<AgentLogDto> Items, int Total);

public sealed record UsageAnalyticsInput(
    DateTime From,
    DateTime To);

public sealed record UsageAnalyticsPointDto(
    DateTime Date,
    long Tokens,
    long Credits);

public sealed record UsageCostBreakdownDto(
    long TotalCents,
    long IncludedCents,
    long OnDemandCents,
    string Currency,
    bool Estimated);

public sealed record UsageAnalyticsDto(
    DateTime From,
    DateTime To,
    long TotalTokens,
    long TotalCredits,
    UsageCostBreakdownDto Cost,
    IReadOnlyList<UsageAnalyticsPointDto> Points);

public sealed record AppendAgentLogInput(
    Guid AgentId,
    AgentLogType Type,
    string Content,
    string? Tool = null,
    string? Integration = null,
    string? Channel = null,
    string? CorrelationId = null);

// Audit types (merged from Entities/Audit)
public sealed record AuditEntry(
    Guid Id,
    Guid? AgentId,
    Guid? UserId,
    string SkillName,
    string Action,
    string ParamsJson,
    string? ResultSummary,
    long DurationMs,
    DateTime Timestamp);

public sealed record AuditLogPage(IReadOnlyList<AuditEntry> Items, int Total);

public static class AgentLogMapper
{
    public static AgentLogDto ToDto(this AgentLogRecord r, string? agentName = null) => new(
        r.Id, r.AgentId, agentName, r.Time, r.Type,
        r.Tool, r.Integration, r.Channel, r.ChannelConnectionId, r.Content,
        r.Usage.DurationMs, r.Usage.InputTokens, r.Usage.OutputTokens, r.CorrelationId);
}
