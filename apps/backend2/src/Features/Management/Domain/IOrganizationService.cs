namespace OffceOs.Domain.Features.Management;

public interface IOrganizationService
{
    Task<OrganizationOverview> GetCurrentOverviewAsync(Guid callerUserId, CancellationToken ct = default);
    Task<OrganizationRecord?> GetOwnedOrganizationAsync(Guid callerUserId, CancellationToken ct = default);
    Task<IReadOnlyList<OrganizationRecord>> ListJoinedOrganizationsAsync(Guid callerUserId, CancellationToken ct = default);
    Task<OrganizationRecord> EnsureOrganizationAsync(Guid callerUserId, string callerEmail, string? callerName, CancellationToken ct = default);
    Task<OrganizationRecord> GetCurrentOrganizationAsync(Guid callerUserId, CancellationToken ct = default);
    Task<OrganizationRecord> CreateOrganizationAsync(Guid callerUserId, string callerEmail, string? callerName, string name, CancellationToken ct = default);
    Task<OrganizationRecord> SelectOrganizationAsync(Guid callerUserId, Guid organizationId, CancellationToken ct = default);
    Task<OrgMemberRecord> InviteMemberAsync(Guid callerUserId, string memberEmail, string? role, CancellationToken ct = default);
    Task<IReadOnlyList<OrganizationInviteRecord>> ListPendingInvitesAsync(Guid callerUserId, string callerEmail, CancellationToken ct = default);
    Task<OrgMemberRecord> AcceptInviteAsync(Guid callerUserId, string callerEmail, Guid memberId, CancellationToken ct = default);
    Task<bool> DeclineInviteAsync(Guid callerUserId, string callerEmail, Guid memberId, CancellationToken ct = default);
    Task<bool> RemoveMemberAsync(Guid callerUserId, Guid memberId, CancellationToken ct = default);
    Task<OrganizationRecord> RenameAsync(Guid callerUserId, string name, CancellationToken ct = default);
    Task<IReadOnlyList<OrgMemberRecord>> ListMembersAsync(Guid callerUserId, CancellationToken ct = default);
}

public sealed record OrganizationOverview(
    OrganizationRecord Organization,
    IReadOnlyList<OrgMemberRecord> Members);
