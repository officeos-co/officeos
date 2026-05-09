namespace OffceOs.Application.Features.Context;

internal sealed class MemoryStoreService : IMemoryStoreService
{
    private readonly IMemoryStoreRepository _memoryStoreRepository;

    public MemoryStoreService(IMemoryStoreRepository memoryStores)
    {
        _memoryStoreRepository = memoryStores;
    }

    public Task<MemoryStoreRecord> CreateAsync(Guid ownerId, string? displayName, CancellationToken ct = default) =>
        _memoryStoreRepository.CreateAsync(MemoryStoreRecord.Create(ownerId, displayName ?? "Memory Store"), ct);

    public Task<bool> DeleteAsync(Guid id, Guid ownerId, CancellationToken ct = default) =>
        _memoryStoreRepository.DeleteAsync(id, ownerId, ct);

    public Task<MemoryStoreEntryRecord> UpsertEntryAsync(
        Guid memoryStoreId,
        Guid ownerId,
        string key,
        string content,
        CancellationToken ct = default) =>
        _memoryStoreRepository.UpsertEntryAsync(memoryStoreId, ownerId, key, content, ct);

    public Task<bool> DeleteEntryAsync(
        Guid memoryStoreId,
        Guid ownerId,
        string key,
        CancellationToken ct = default) =>
        _memoryStoreRepository.DeleteEntryAsync(memoryStoreId, ownerId, key, ct);
}
