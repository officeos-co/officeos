namespace OffceOs.Domain.Features.Management;

public interface IOrganizationRepository
{
    Task<OrganizationRecord> GetOrCreateDefaultAsync(
        Guid ownerUserId,
        string ownerEmail,
        string? ownerName,
        CancellationToken ct = default);

    Task<OrganizationRecord?> GetByAsync(OrganizationFilter filter, CancellationToken ct = default);

    Task<IReadOnlyList<OrgMemberRecord>> ListMembersAsync(
        Guid organizationId,
        CancellationToken ct = default);

    Task<IReadOnlyList<OrganizationInviteRecord>> ListPendingInvitesForEmailAsync(
        string email,
        CancellationToken ct = default);

    Task<OrgMemberRecord> AddMemberAsync(OrgMemberRecord member, CancellationToken ct = default);

    Task<OrgMemberRecord> AcceptInviteAsync(Guid memberId, Guid userId, string email, CancellationToken ct = default);

    Task<bool> RemoveMemberAsync(Guid memberId, CancellationToken ct = default);

    Task<OrganizationRecord> RenameAsync(
        Guid organizationId,
        string name,
        CancellationToken ct = default);
}
