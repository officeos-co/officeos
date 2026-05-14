namespace OffceOs.Domain.Features.Management;

public interface IOrganizationPolicyService
{
    Task<OrganizationPolicyProfileRecord?> GetEffectiveForWorkspaceAsync(Guid? workspaceId, CancellationToken ct = default);
    Task<OrganizationPolicyProfileRecord> GetOrCreateAsync(Guid actorUserId, Guid organizationId, CancellationToken ct = default);
    Task<OrganizationPolicyProfileRecord> UpdateAsync(Guid actorUserId, OrganizationPolicyProfileRecord profile, CancellationToken ct = default);
}
