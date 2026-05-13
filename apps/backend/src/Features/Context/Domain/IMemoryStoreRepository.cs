namespace OffceOs.Domain.Features.Context;

public interface IMemoryStoreRepository
{
    Task<IReadOnlyList<MemoryStoreRecord>> ListAsync(Guid? ownerId, Guid workspaceId, CancellationToken ct = default);
    Task<MemoryStoreRecord?> GetAsync(Guid id, Guid? ownerId, Guid? workspaceId = null, CancellationToken ct = default);
    Task<MemoryStoreRecord> CreateAsync(MemoryStoreRecord store, CancellationToken ct = default);
    Task<MemoryStoreRecord?> UpdateAsync(Guid id, Guid? ownerId, Guid workspaceId, string displayName, CancellationToken ct = default);
    Task<bool> DeleteAsync(Guid id, Guid? ownerId, Guid workspaceId, CancellationToken ct = default);
    Task<IReadOnlyList<MemoryStoreEntryRecord>> ListEntriesAsync(Guid memoryStoreId, Guid? ownerId, Guid workspaceId, CancellationToken ct = default);
    Task<MemoryStoreEntryRecord> UpsertEntryAsync(Guid memoryStoreId, Guid? ownerId, Guid workspaceId, string key, string content, CancellationToken ct = default);
    Task<bool> DeleteEntryAsync(Guid memoryStoreId, Guid? ownerId, Guid workspaceId, string key, CancellationToken ct = default);
    Task<IReadOnlyList<MemoryStoreEntryRecord>> ListEntriesForStoreAsync(Guid memoryStoreId, CancellationToken ct = default);
    Task<MemoryStoreEntryRecord> UpsertEntryForStoreAsync(Guid memoryStoreId, string key, string content, CancellationToken ct = default);
    Task<bool> DeleteEntryForStoreAsync(Guid memoryStoreId, string key, CancellationToken ct = default);
}
