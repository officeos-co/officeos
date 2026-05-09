namespace OffceOs.Domain.Features.Agents;

public interface IAgentResourceRepository
{
    Task<IReadOnlyList<BrowserResourceRecord>> ListBrowserResourcesAsync(Guid ownerId, Guid workspaceId, CancellationToken ct = default);
    Task<BrowserResourceRecord?> GetBrowserResourceAsync(Guid id, Guid ownerId, Guid? workspaceId = null, CancellationToken ct = default);
    Task<BrowserResourceRecord> CreateBrowserResourceAsync(BrowserResourceRecord resource, CancellationToken ct = default);
    Task<bool> DeleteBrowserResourceAsync(Guid id, Guid ownerId, Guid workspaceId, CancellationToken ct = default);
    Task SetBrowserCurrentAgentAsync(Guid browserResourceId, Guid agentId, CancellationToken ct = default);

    Task AttachToSessionAsync(AgentSessionResourceAttachmentRecord attachment, CancellationToken ct = default);
    Task<IReadOnlyList<AgentSessionResourceAttachmentRecord>> ListSessionAttachmentsAsync(Guid sessionId, CancellationToken ct = default);
    Task<AgentSessionResourceAttachmentRecord?> GetActiveMemoryStoreAttachmentAsync(Guid agentId, CancellationToken ct = default);
}
