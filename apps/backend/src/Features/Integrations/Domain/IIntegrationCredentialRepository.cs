namespace OffceOs.Domain.Features.Integrations;

public interface IIntegrationCredentialRepository
{
    Task<IntegrationCredentialRecord?> GetByAsync(IntegrationCredentialFilter filter, CancellationToken ct = default);
    Task UpsertAsync(IntegrationCredentialRecord credential, CancellationToken ct = default);
    Task DeleteAsync(Guid ownerId, string integrationName, CancellationToken ct = default);
}
