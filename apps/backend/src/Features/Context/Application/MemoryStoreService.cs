namespace EnterpriseAgentOs.Application.Features.Context;

internal sealed class MemoryStoreService : IMemoryStoreService
{
    private readonly IMemoryStoreRepository _memoryStores;

    public MemoryStoreService(IMemoryStoreRepository memoryStores)
    {
        _memoryStores = memoryStores;
    }

    public Task<MemoryStoreRecord> CreateAsync(Guid ownerId, string? displayName, CancellationToken ct = default) =>
        _memoryStores.CreateAsync(MemoryStoreRecord.Create(ownerId, displayName ?? "Memory Store"), ct);

    public Task<bool> DeleteAsync(Guid id, Guid ownerId, CancellationToken ct = default) =>
        _memoryStores.DeleteAsync(id, ownerId, ct);

    public Task<MemoryStoreEntryRecord> UpsertEntryAsync(
        Guid memoryStoreId,
        Guid ownerId,
        string key,
        string content,
        CancellationToken ct = default) =>
        _memoryStores.UpsertEntryAsync(memoryStoreId, ownerId, key, content, ct);

    public Task<bool> DeleteEntryAsync(
        Guid memoryStoreId,
        Guid ownerId,
        string key,
        CancellationToken ct = default) =>
        _memoryStores.DeleteEntryAsync(memoryStoreId, ownerId, key, ct);
}
