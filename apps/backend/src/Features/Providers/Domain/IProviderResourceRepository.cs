namespace OffceOs.Domain.Features.Providers;

public interface IProviderResourceRepository
{
    Task<IReadOnlyList<ProviderResourceRecord>> ListAsync(Guid workspaceId, CancellationToken ct = default);
    Task<ProviderResourceRecord?> GetByNameAsync(Guid workspaceId, string name, CancellationToken ct = default);
    Task<ProviderResourceRecord> UpsertAsync(ProviderResourceRecord record, CancellationToken ct = default);
    Task<bool> DeleteAsync(Guid workspaceId, string name, CancellationToken ct = default);
}
