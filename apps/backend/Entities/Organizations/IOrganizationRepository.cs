namespace EnterpriseAgentOs.Api.Entities.Organizations;

public interface IOrganizationRepository
{
    Task<EnterpriseAgentOs.Api.Database.Models.OrganizationRecord> GetOrCreateDefaultAsync(
        Guid ownerUserId,
        string ownerEmail,
        string? ownerName,
        CancellationToken ct = default);

    Task<EnterpriseAgentOs.Api.Database.Models.OrganizationRecord?> GetByIdAsync(
        Guid id,
        CancellationToken ct = default);

    Task<IReadOnlyList<EnterpriseAgentOs.Api.Database.Models.OrgMemberRecord>> ListMembersAsync(
        Guid organizationId,
        CancellationToken ct = default);

    Task<EnterpriseAgentOs.Api.Database.Models.OrgMemberRecord> AddMemberAsync(
        Guid organizationId,
        string email,
        string role,
        string status,
        Guid? userId,
        CancellationToken ct = default);

    Task<bool> RemoveMemberAsync(Guid memberId, CancellationToken ct = default);

    Task<EnterpriseAgentOs.Api.Database.Models.OrganizationRecord> RenameAsync(
        Guid organizationId,
        string name,
        CancellationToken ct = default);
}
