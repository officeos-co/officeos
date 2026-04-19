namespace EnterpriseAgentOs.Domain.Interfaces.Organizations;

public interface IOrganizationRepository
{
    Task<OrganizationRecord> GetOrCreateDefaultAsync(
        Guid ownerUserId,
        string ownerEmail,
        string? ownerName,
        CancellationToken ct = default);

    Task<OrganizationRecord?> GetByIdAsync(
        Guid id,
        CancellationToken ct = default);

    Task<IReadOnlyList<OrgMemberRecord>> ListMembersAsync(
        Guid organizationId,
        CancellationToken ct = default);

    Task<OrgMemberRecord> AddMemberAsync(
        Guid organizationId,
        string email,
        string role,
        string status,
        Guid? userId,
        CancellationToken ct = default);

    Task<bool> RemoveMemberAsync(Guid memberId, CancellationToken ct = default);

    Task<OrganizationRecord> RenameAsync(
        Guid organizationId,
        string name,
        CancellationToken ct = default);
}
