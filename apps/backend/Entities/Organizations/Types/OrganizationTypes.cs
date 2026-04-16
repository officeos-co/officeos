namespace EnterpriseAgentOs.Api.Entities.Organizations.Types;

public sealed record OrganizationPayload(
    Guid Id,
    string Name,
    Guid OwnerUserId,
    DateTime CreatedAt,
    IReadOnlyList<OrgMemberPayload> Members);

public sealed record OrgMemberPayload(
    Guid Id,
    Guid OrganizationId,
    Guid? UserId,
    string Email,
    string? Name,
    string Role,
    string Status,
    DateTime CreatedAt);

public sealed record InviteMemberInput(
    string Email,
    string? Role);

public sealed record RenameOrgInput(
    string Name);
