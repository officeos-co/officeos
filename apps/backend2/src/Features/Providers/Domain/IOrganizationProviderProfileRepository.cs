namespace OffceOs.Domain.Features.Providers;

public interface IOrganizationProviderProfileRepository
{
    Task<IReadOnlyList<OrganizationProviderProfileRecord>> ListAsync(OrganizationProviderProfileFilter filter, CancellationToken ct = default);
    Task<OrganizationProviderProfileRecord?> GetByAsync(OrganizationProviderProfileFilter filter, CancellationToken ct = default);
    Task<OrganizationProviderProfileRecord> UpsertAsync(OrganizationProviderProfileRecord record, CancellationToken ct = default);
    Task<bool> DeleteAsync(OrganizationProviderProfileFilter filter, CancellationToken ct = default);
}
