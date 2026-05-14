namespace OffceOs.Application.Features.Management;

// ── Export DTOs ───────────────────────────────────────────────────────────────

public record GdprUserExport(
    Guid Id,
    string Email,
    string? Name,
    DateTime CreatedAt,
    DateTime LastLoginAt);

public record GdprAgentExport(
    Guid Id,
    string Name,
    string Provider,
    string? Model,
    string Status,
    DateTime CreatedAt);

public record GdprConversationExport(
    Guid Id,
    Guid AgentId,
    string Role,
    string Content,
    string? SessionId,
    DateTime CreatedAt);

public record GdprAuditEntryExport(
    Guid Id,
    Guid AgentId,
    string SkillName,
    string Action,
    string ParamsJson,
    string? ResultSummary,
    long DurationMs,
    DateTime Timestamp);

public record GdprExport(
    GdprUserExport User,
    List<GdprAgentExport> Agents,
    List<GdprConversationExport> Conversations,
    List<GdprAuditEntryExport> AuditEntries);
