namespace EnterpriseAgentOs.Domain.Features.Context;

public interface IMemoryStoreService
{
    Task<MemoryStoreRecord> CreateAsync(Guid ownerId, string? displayName, CancellationToken ct = default);
    Task<bool> DeleteAsync(Guid id, Guid ownerId, CancellationToken ct = default);
    Task<MemoryStoreEntryRecord> UpsertEntryAsync(Guid memoryStoreId, Guid ownerId, string key, string content, CancellationToken ct = default);
    Task<bool> DeleteEntryAsync(Guid memoryStoreId, Guid ownerId, string key, CancellationToken ct = default);
}
