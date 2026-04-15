using System.Text.Json;

namespace EnterpriseAgentOs.Api.Entities.ApprovalQueue;

public sealed record ApprovalRequestDto(
    Guid Id,
    Guid AgentId,
    string SkillName,
    string Action,
    string ParamsJson,
    string Status,
    DateTime RequestedAt,
    DateTime? DecidedAt,
    Guid? DecidedByUserId,
    string? ResultJson);

public sealed record ApprovalRequestAgentDto(
    Guid Id,
    Guid AgentId,
    string SkillName,
    string Action,
    string Status,
    DateTime RequestedAt,
    DateTime? DecidedAt,
    JsonElement? Result);

public sealed record SetApprovalOverrideRequest(bool RequiresApproval);
