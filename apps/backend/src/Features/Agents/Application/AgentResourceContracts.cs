namespace OffceOs.Application.Features.Agents;

public interface IAgentResourceService
{
    Task<BrowserResourceRecord> CreateBrowserResourceAsync(Guid ownerId, Guid workspaceId, string? displayName, CancellationToken ct = default);
    Task<bool> DeleteBrowserResourceAsync(Guid id, Guid ownerId, Guid workspaceId, CancellationToken ct = default);
    Task<IReadOnlyList<AgentSessionResourceAttachmentRecord>> ListSessionAttachmentsAsync(Guid sessionId, Guid ownerId, Guid workspaceId, CancellationToken ct = default);
}
