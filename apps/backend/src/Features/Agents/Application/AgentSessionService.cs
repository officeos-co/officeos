using OffceOs.Features.Agents.Domain;

namespace OffceOs.Features.Agents.Application;

internal sealed class AgentSessionService : IAgentSessionService
{
    private readonly IAgentRepository _agentRepository;
    private readonly IAgentSessionRepository _agentSessionRepository;

    public AgentSessionService(
        IAgentRepository agentRepository,
        IAgentSessionRepository agentSessionRepository)
    {
        _agentRepository = agentRepository;
        _agentSessionRepository = agentSessionRepository;
    }

    public async Task<IReadOnlyList<AgentSessionRecord>> ListByAgentAsync(
        Guid agentId,
        Guid ownerId,
        int limit = 20,
        CancellationToken ct = default)
    {
        await EnsureAgentOwnedAsync(agentId, ownerId, ct);
        return await _agentSessionRepository.ListByAgentAsync(agentId, limit, ct);
    }

    public Task<AgentSessionRecord?> GetForOwnerAsync(Guid sessionId, Guid ownerId, CancellationToken ct = default)
        => _agentSessionRepository.GetByAsync(new AgentSessionFilter { Id = sessionId, OwnerId = ownerId }, ct);

    public async Task<AgentSessionRecord> CreateRunAsync(CreateAgentSessionRequest request, Guid ownerId, CancellationToken ct = default)
    {
        var agent = await GetOwnedAgentAsync(request.AgentId, ownerId, ct);
        var session = AgentSessionRecord.CreateRun(
            agent,
            request.Input,
            request.Purpose,
            request.Source,
            request.CorrelationId,
            request.RoutineId,
            request.TriggerId,
            request.DefinitionId,
            request.TriggerPayloadJson,
            request.Repository);

        return await _agentSessionRepository.CreateAsync(session, ct);
    }

    public async Task MarkRunningAsync(Guid sessionId, string sandboxId, string serviceUrl, CancellationToken ct = default)
    {
        var session = await GetSessionAsync(sessionId, ct);
        session.MarkRunning(sandboxId, serviceUrl, DateTime.UtcNow);
        await _agentSessionRepository.SaveAsync(session, ct);
    }

    public async Task MarkCompletedAsync(Guid sessionId, CancellationToken ct = default)
    {
        var session = await GetSessionAsync(sessionId, ct);
        session.MarkCompleted(DateTime.UtcNow);
        await _agentSessionRepository.SaveAsync(session, ct);
    }

    public async Task MarkFailedAsync(Guid sessionId, string error, CancellationToken ct = default)
    {
        var session = await GetSessionAsync(sessionId, ct);
        session.MarkFailed(error, DateTime.UtcNow);
        await _agentSessionRepository.SaveAsync(session, ct);
    }

    private async Task EnsureAgentOwnedAsync(Guid agentId, Guid ownerId, CancellationToken ct)
    {
        _ = await GetOwnedAgentAsync(agentId, ownerId, ct);
    }

    private async Task<AgentRecord> GetOwnedAgentAsync(Guid agentId, Guid ownerId, CancellationToken ct)
    {
        var agent = await _agentRepository.GetByAsync(new AgentFilter { Id = agentId, OwnerId = ownerId }, ct);
        if (agent is null)
            throw new InvalidOperationException("Agent not found.");
        return agent;
    }

    private async Task<AgentSessionRecord> GetSessionAsync(Guid sessionId, CancellationToken ct)
    {
        var session = await _agentSessionRepository.GetByAsync(new AgentSessionFilter { Id = sessionId }, ct);
        return session ?? throw new InvalidOperationException("Session not found.");
    }
}
