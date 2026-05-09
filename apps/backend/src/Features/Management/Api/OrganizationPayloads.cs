namespace OffceOs.Api.Features.Management;

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
