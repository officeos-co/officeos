namespace EnterpriseAgentOs.Api.Entities.Audit.GraphQL;

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
