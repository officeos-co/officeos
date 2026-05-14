namespace OffceOs.Domain.Features.Agents;

public interface IAgentResourceRepository
{
    Task AttachToSessionAsync(AgentSessionResourceAttachmentRecord attachment, CancellationToken ct = default);
    Task<IReadOnlyList<AgentSessionResourceAttachmentRecord>> ListSessionAttachmentsAsync(Guid sessionId, CancellationToken ct = default);
    Task<AgentSessionResourceAttachmentRecord?> GetActiveMemoryStoreAttachmentAsync(Guid agentId, CancellationToken ct = default);
}
