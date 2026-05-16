namespace OffceOs.Features.Context.Domain;

public interface IMemoryStoreService
{
    Task<MemoryStoreRecord> CreateAsync(Guid ownerId, Guid workspaceId, string? displayName, CancellationToken ct = default);
    Task<bool> DeleteAsync(Guid id, Guid ownerId, Guid workspaceId, CancellationToken ct = default);
    Task<MemoryStoreEntryRecord> UpsertEntryAsync(Guid memoryStoreId, Guid ownerId, Guid workspaceId, string key, string content, CancellationToken ct = default);
    Task<bool> DeleteEntryAsync(Guid memoryStoreId, Guid ownerId, Guid workspaceId, string key, CancellationToken ct = default);
}
