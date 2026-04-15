namespace EnterpriseAgentOs.Api.Entities.Audit;

public sealed record AuditLogEntryDto(
    Guid Id,
    Guid AgentId,
    Guid? UserId,
    string SkillName,
    string Action,
    string ParamsJson,
    string? ResultSummary,
    long DurationMs,
    DateTime Timestamp);

public sealed record AuditLogResponse(List<AuditLogEntryDto> Items, int Total);
