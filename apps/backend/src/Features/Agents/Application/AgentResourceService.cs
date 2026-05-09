namespace OffceOs.Application.Features.Agents;

internal sealed class AgentResourceService : IAgentResourceService
{
    private readonly IAgentResourceRepository _agentResourceRepository;
    private readonly IAgentSessionRepository _agentSessionRepository;
    private readonly IAgentRepository _agentRepository;

    public AgentResourceService(
        IAgentResourceRepository resources,
        IAgentSessionRepository sessions,
        IAgentRepository agents)
    {
        _agentResourceRepository = resources;
        _agentSessionRepository = sessions;
        _agentRepository = agents;
    }

    public Task<BrowserResourceRecord> CreateBrowserResourceAsync(
        Guid ownerId,
        string? displayName,
        CancellationToken ct = default) =>
        _agentResourceRepository.CreateBrowserResourceAsync(BrowserResourceRecord.Create(ownerId, displayName ?? "Browser"), ct);

    public Task<bool> DeleteBrowserResourceAsync(Guid id, Guid ownerId, CancellationToken ct = default) =>
        _agentResourceRepository.DeleteBrowserResourceAsync(id, ownerId, ct);

    public async Task<IReadOnlyList<AgentSessionResourceAttachmentRecord>> ListSessionAttachmentsAsync(
        Guid sessionId,
        Guid ownerId,
        CancellationToken ct = default)
    {
        var session = await _agentSessionRepository.GetByAsync(new AgentSessionFilter { Id = sessionId }, ct);
        if (session is null)
            throw new InvalidOperationException("Session not found.");

        var agent = await _agentRepository.GetByAsync(new AgentFilter { Id = session.AgentId, OwnerId = ownerId }, ct);
        if (agent is null)
            throw new InvalidOperationException("Session not found.");

        return await _agentResourceRepository.ListSessionAttachmentsAsync(sessionId, ct);
    }
}
