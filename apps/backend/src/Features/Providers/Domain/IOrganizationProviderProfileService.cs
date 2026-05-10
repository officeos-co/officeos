namespace OffceOs.Domain.Features.Providers;

public interface IOrganizationProviderProfileService
{
    Task<IReadOnlyList<OrganizationProviderProfileRecord>> ListAsync(Guid actorUserId, Guid organizationId, CancellationToken ct = default);
    Task<OrganizationProviderProfileRecord> SaveAsync(Guid actorUserId, Guid organizationId, string provider, string displayName, IReadOnlyList<string> allowedModels, string apiKey, bool enabled, CancellationToken ct = default);
    Task<OrganizationProviderProfileRecord> SaveNativeAuthAsync(Guid actorUserId, Guid organizationId, string provider, string displayName, IReadOnlyList<string> allowedModels, ProviderAuthKind authKind, IReadOnlyDictionary<string, string> credentials, bool enabled, CancellationToken ct = default);
    Task<bool> DeleteAsync(Guid actorUserId, Guid organizationId, string provider, CancellationToken ct = default);
}
