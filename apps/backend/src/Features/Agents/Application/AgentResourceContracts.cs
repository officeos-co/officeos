using OffceOs.Features.Agents.Domain;

namespace OffceOs.Features.Agents.Application;

public interface IAgentResourceService
{
    Task<IReadOnlyList<AgentSessionResourceAttachmentRecord>> ListSessionAttachmentsAsync(Guid sessionId, Guid ownerId, Guid workspaceId, CancellationToken ct = default);
}
