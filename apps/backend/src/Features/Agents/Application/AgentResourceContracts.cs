namespace EnterpriseAgentOs.Application.Features.Agents;

public interface IAgentResourceService
{
    Task<BrowserResourceRecord> CreateBrowserResourceAsync(Guid ownerId, string? displayName, CancellationToken ct = default);
    Task<bool> DeleteBrowserResourceAsync(Guid id, Guid ownerId, CancellationToken ct = default);
    Task<IReadOnlyList<AgentSessionResourceAttachmentRecord>> ListSessionAttachmentsAsync(Guid sessionId, Guid ownerId, CancellationToken ct = default);
}
