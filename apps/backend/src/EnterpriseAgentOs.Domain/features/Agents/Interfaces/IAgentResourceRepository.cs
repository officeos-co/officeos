namespace EnterpriseAgentOs.Domain.Features.Agents;

public interface IAgentResourceRepository
{
    Task<IReadOnlyList<BrowserResourceRecord>> ListBrowserResourcesAsync(Guid ownerId, CancellationToken ct = default);
    Task<BrowserResourceRecord?> GetBrowserResourceAsync(Guid id, Guid ownerId, CancellationToken ct = default);
    Task<BrowserResourceRecord> CreateBrowserResourceAsync(BrowserResourceRecord resource, CancellationToken ct = default);
    Task<bool> DeleteBrowserResourceAsync(Guid id, Guid ownerId, CancellationToken ct = default);
    Task SetBrowserCurrentAgentAsync(Guid browserResourceId, Guid agentId, CancellationToken ct = default);

    Task<IReadOnlyList<MemoryStoreRecord>> ListMemoryStoresAsync(Guid ownerId, CancellationToken ct = default);
    Task<MemoryStoreRecord?> GetMemoryStoreAsync(Guid id, Guid ownerId, CancellationToken ct = default);
    Task<MemoryStoreRecord> CreateMemoryStoreAsync(MemoryStoreRecord store, CancellationToken ct = default);
    Task<bool> DeleteMemoryStoreAsync(Guid id, Guid ownerId, CancellationToken ct = default);
    Task<IReadOnlyList<MemoryStoreEntryRecord>> ListMemoryStoreEntriesAsync(Guid memoryStoreId, Guid ownerId, CancellationToken ct = default);
    Task<MemoryStoreEntryRecord> UpsertMemoryStoreEntryAsync(Guid memoryStoreId, Guid ownerId, string key, string content, CancellationToken ct = default);
    Task<bool> DeleteMemoryStoreEntryAsync(Guid memoryStoreId, Guid ownerId, string key, CancellationToken ct = default);
    Task<IReadOnlyList<MemoryStoreEntryRecord>?> ListActiveMemoryStoreEntriesAsync(Guid agentId, CancellationToken ct = default);
    Task<MemoryStoreEntryRecord?> UpsertActiveMemoryStoreEntryAsync(Guid agentId, string key, string content, CancellationToken ct = default);
    Task<bool?> DeleteActiveMemoryStoreEntryAsync(Guid agentId, string key, CancellationToken ct = default);

    Task AttachToSessionAsync(AgentSessionResourceAttachmentRecord attachment, CancellationToken ct = default);
    Task<IReadOnlyList<AgentSessionResourceAttachmentRecord>> ListSessionAttachmentsAsync(Guid sessionId, CancellationToken ct = default);
    Task<AgentSessionResourceAttachmentRecord?> GetActiveMemoryStoreAttachmentAsync(Guid agentId, CancellationToken ct = default);
}
