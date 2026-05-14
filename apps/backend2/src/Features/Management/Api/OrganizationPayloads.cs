namespace OffceOs.Api.Features.Management;

public sealed record OrganizationPayload(
    Guid Id,
    string Name,
    string Kind,
    Guid OwnerUserId,
    DateTime CreatedAt,
    IReadOnlyList<OrgMemberRecord> Members);

public sealed record OrganizationSummaryPayload(
    Guid Id,
    string Name,
    string Kind,
    Guid OwnerUserId,
    DateTime CreatedAt);

public sealed record OrganizationContextPayload(
    OrganizationPayload CurrentOrganization,
    OrganizationSummaryPayload? OwnedOrganization,
    IReadOnlyList<OrganizationSummaryPayload> JoinedOrganizations,
    IReadOnlyList<OrganizationInvitePayload> PendingInvites);

public sealed record OrganizationInvitePayload(
    Guid Id,
    Guid OrganizationId,
    string OrganizationName,
    string Email,
    string Role,
    DateTime CreatedAt);

public sealed record InviteMemberInput(
    string Email,
    string? Role);

public sealed record RenameOrgInput(
    string Name);

public sealed record CreateOrganizationInput(
    string Name);

internal static class OrganizationGraphQLMapper
{
    public static OrganizationPayload ToPayload(OrganizationOverview overview) => new(
        overview.Organization.Id,
        overview.Organization.Name,
        overview.Organization.Kind.ToStorageString(),
        overview.Organization.OwnerUserId,
        overview.Organization.CreatedAt,
        overview.Members);

    public static OrganizationSummaryPayload ToSummaryPayload(OrganizationRecord organization) => new(
        organization.Id,
        organization.Name,
        organization.Kind.ToStorageString(),
        organization.OwnerUserId,
        organization.CreatedAt);
}
