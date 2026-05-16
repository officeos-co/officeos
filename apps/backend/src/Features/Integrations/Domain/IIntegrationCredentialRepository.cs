namespace OffceOs.Features.Integrations.Domain;

public interface IIntegrationCredentialRepository
{
    Task<IReadOnlyList<IntegrationCredentialRecord>> ListAsync(IntegrationCredentialFilter filter, CancellationToken ct = default);
    Task<IntegrationCredentialRecord?> GetByAsync(IntegrationCredentialFilter filter, CancellationToken ct = default);
    Task UpsertAsync(IntegrationCredentialRecord credential, CancellationToken ct = default);
    Task ArchiveAsync(Guid workspaceId, string integrationName, CancellationToken ct = default);
    Task DeleteAsync(Guid ownerId, string integrationName, Guid? workspaceId = null, CancellationToken ct = default);
    Task MarkUsedAsync(Guid id, DateTime usedAt, CancellationToken ct = default);
}
