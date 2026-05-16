using OffceOs.Domain.Features.Context;

namespace OffceOs.Application.Features.Context;

internal sealed class MemoryStoreService : IMemoryStoreService
{
    private readonly IMemoryStoreRepository _memoryStoreRepository;

    public MemoryStoreService(IMemoryStoreRepository memoryStores)
    {
        _memoryStoreRepository = memoryStores;
    }

    public Task<MemoryStoreRecord> CreateAsync(Guid ownerId, Guid workspaceId, string? displayName, CancellationToken ct = default) =>
        _memoryStoreRepository.CreateAsync(MemoryStoreRecord.Create(ownerId, workspaceId, displayName ?? "Memory Store"), ct);

    public Task<bool> DeleteAsync(Guid id, Guid ownerId, Guid workspaceId, CancellationToken ct = default) =>
        _memoryStoreRepository.DeleteAsync(id, null, workspaceId, ct);

    public Task<MemoryStoreEntryRecord> UpsertEntryAsync(
        Guid memoryStoreId,
        Guid ownerId,
        Guid workspaceId,
        string key,
        string content,
        CancellationToken ct = default) =>
        _memoryStoreRepository.UpsertEntryAsync(memoryStoreId, null, workspaceId, key, content, ct);

    public Task<bool> DeleteEntryAsync(
        Guid memoryStoreId,
        Guid ownerId,
        Guid workspaceId,
        string key,
        CancellationToken ct = default) =>
        _memoryStoreRepository.DeleteEntryAsync(memoryStoreId, null, workspaceId, key, ct);
}
