namespace EnterpriseAgentOs.Domain.Interfaces.Organizations;

public interface IOrganizationRepository
{
    Task<EnterpriseAgentOs.Domain.Models.OrganizationRecord> GetOrCreateDefaultAsync(
        Guid ownerUserId,
        string ownerEmail,
        string? ownerName,
        CancellationToken ct = default);

    Task<EnterpriseAgentOs.Domain.Models.OrganizationRecord?> GetByIdAsync(
        Guid id,
        CancellationToken ct = default);

    Task<IReadOnlyList<EnterpriseAgentOs.Domain.Models.OrgMemberRecord>> ListMembersAsync(
        Guid organizationId,
        CancellationToken ct = default);

    Task<EnterpriseAgentOs.Domain.Models.OrgMemberRecord> AddMemberAsync(
        Guid organizationId,
        string email,
        string role,
        string status,
        Guid? userId,
        CancellationToken ct = default);

    Task<bool> RemoveMemberAsync(Guid memberId, CancellationToken ct = default);

    Task<EnterpriseAgentOs.Domain.Models.OrganizationRecord> RenameAsync(
        Guid organizationId,
        string name,
        CancellationToken ct = default);
}
