namespace OffceOs.Application.Features.Agents;

public interface IAgentResourceService
{
    Task<IReadOnlyList<AgentSessionResourceAttachmentRecord>> ListSessionAttachmentsAsync(Guid sessionId, Guid ownerId, Guid workspaceId, CancellationToken ct = default);
}
