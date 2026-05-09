namespace EnterpriseAgentOs.Application.Features.Agents;

internal sealed class AgentResourceService : IAgentResourceService
{
    private readonly IAgentResourceRepository _resources;
    private readonly IAgentSessionRepository _sessions;
    private readonly IAgentRepository _agents;

    public AgentResourceService(
        IAgentResourceRepository resources,
        IAgentSessionRepository sessions,
        IAgentRepository agents)
    {
        _resources = resources;
        _sessions = sessions;
        _agents = agents;
    }

    public Task<BrowserResourceRecord> CreateBrowserResourceAsync(
        Guid ownerId,
        string? displayName,
        CancellationToken ct = default) =>
        _resources.CreateBrowserResourceAsync(BrowserResourceRecord.Create(ownerId, displayName ?? "Browser"), ct);

    public Task<bool> DeleteBrowserResourceAsync(Guid id, Guid ownerId, CancellationToken ct = default) =>
        _resources.DeleteBrowserResourceAsync(id, ownerId, ct);

    public async Task<IReadOnlyList<AgentSessionResourceAttachmentRecord>> ListSessionAttachmentsAsync(
        Guid sessionId,
        Guid ownerId,
        CancellationToken ct = default)
    {
        var session = await _sessions.GetByAsync(new AgentSessionFilter { Id = sessionId }, ct);
        if (session is null)
            throw new InvalidOperationException("Session not found.");

        var agent = await _agents.GetByAsync(new AgentFilter { Id = session.AgentId, OwnerId = ownerId }, ct);
        if (agent is null)
            throw new InvalidOperationException("Session not found.");

        return await _resources.ListSessionAttachmentsAsync(sessionId, ct);
    }
}
