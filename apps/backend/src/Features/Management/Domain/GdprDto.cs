namespace EnterpriseAgentOs.Domain.Features.Management;

// ── Export DTOs ───────────────────────────────────────────────────────────────

public record GdprUserDto(
    Guid Id,
    string Email,
    string? Name,
    DateTime CreatedAt,
    DateTime LastLoginAt);

public record GdprAgentDto(
    Guid Id,
    string Name,
    string Provider,
    string? Model,
    string Status,
    DateTime CreatedAt);

public record GdprConversationDto(
    Guid Id,
    Guid AgentId,
    string Role,
    string Content,
    string? SessionId,
    DateTime CreatedAt);

public record GdprAuditEntryDto(
    Guid Id,
    Guid AgentId,
    string SkillName,
    string Action,
    string ParamsJson,
    string? ResultSummary,
    long DurationMs,
    DateTime Timestamp);

public record GdprExportDto(
    GdprUserDto User,
    List<GdprAgentDto> Agents,
    List<GdprConversationDto> Conversations,
    List<GdprAuditEntryDto> AuditEntries);
