namespace EnterpriseAgentOs.Api.Features.Organizations;

public sealed record OrganizationPayload(
    Guid Id,
    string Name,
    Guid OwnerUserId,
    DateTime CreatedAt,
    IReadOnlyList<OrgMemberRecord> Members);

public sealed record InviteMemberInput(
    string Email,
    string? Role);

public sealed record RenameOrgInput(
    string Name);
