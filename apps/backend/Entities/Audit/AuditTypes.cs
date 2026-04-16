namespace EnterpriseAgentOs.Api.Entities.Audit.Types;

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
