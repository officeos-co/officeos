namespace OffceOs.Domain.Features.Management;

public interface IOrganizationPolicyProfileRepository
{
    Task<OrganizationPolicyProfileRecord?> GetByAsync(OrganizationPolicyProfileFilter filter, CancellationToken ct = default);
    Task<OrganizationPolicyProfileRecord> SaveAsync(OrganizationPolicyProfileRecord record, CancellationToken ct = default);
}
