namespace EnterpriseAgentOs.Api.Entities.AgentLogs;

public sealed record AgentLogDto(
    Guid Id,
    Guid AgentId,
    string? AgentName,
    DateTime Time,
    EnterpriseAgentOs.Api.Database.Models.AgentLogType Type,
    string? Tool,
    string? Integration,
    string? Channel,
    string Content,
    int? DurationMs,
    int? InputTokens,
    int? OutputTokens,
    string? CorrelationId);

public sealed record GlobalLogFiltersInput(
    string? Search = null,
    string? AgentName = null,
    EnterpriseAgentOs.Api.Database.Models.AgentLogType? Type = null,
    int Skip = 0,
    int Limit = 50);

public sealed record GlobalLogsPage(IReadOnlyList<AgentLogDto> Items, int Total);

public sealed record AppendAgentLogInput(
    Guid AgentId,
    EnterpriseAgentOs.Api.Database.Models.AgentLogType Type,
    string Content,
    string? Tool = null,
    string? Integration = null,
    string? Channel = null,
    string? CorrelationId = null);

// Audit types (merged from Entities/Audit)
public sealed record AuditEntry(
    Guid Id,
    Guid AgentId,
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
    public static AgentLogDto ToDto(this EnterpriseAgentOs.Api.Database.Models.AgentLogRecord r, string? agentName = null) => new(
        r.Id, r.AgentId, agentName, r.Time, r.Type,
        r.Tool, r.Integration, r.Channel, r.Content,
        r.DurationMs, r.InputTokens, r.OutputTokens, r.CorrelationId);
}
