namespace EnterpriseAgentOs.Application.Features.Agents;

internal sealed class AgentSessionService : IAgentSessionService
{
    private readonly IAgentRepository _agents;
    private readonly IAgentSessionRepository _sessions;
    private readonly IAgentLogService _logs;

    public AgentSessionService(
        IAgentRepository agents,
        IAgentSessionRepository sessions,
        IAgentLogService logs)
    {
        _agents = agents;
        _sessions = sessions;
        _logs = logs;
    }

    public async Task<IReadOnlyList<AgentSessionRecord>> ListByAgentAsync(
        Guid agentId,
        Guid ownerId,
        int limit = 20,
        CancellationToken ct = default)
    {
        await EnsureAgentOwnedAsync(agentId, ownerId, ct);
        return await _sessions.ListByAgentAsync(agentId, limit, ct);
    }

    public async Task<AgentSessionRecord?> GetActiveAsync(Guid agentId, Guid ownerId, CancellationToken ct = default)
    {
        await EnsureAgentOwnedAsync(agentId, ownerId, ct);
        return await _sessions.GetByAsync(new AgentSessionFilter { AgentId = agentId, Status = SessionStatus.Active }, ct);
    }

    public async Task<AgentSessionRecord> CreateAsync(Guid agentId, Guid ownerId, CancellationToken ct = default)
    {
        var agent = await GetOwnedAgentAsync(agentId, ownerId, ct);

        var active = await _sessions.GetByAsync(new AgentSessionFilter { AgentId = agentId, Status = SessionStatus.Active }, ct);
        if (active is not null)
        {
            active.End();
            await _sessions.SaveChangesAsync(ct);
        }

        var isFirst = await _sessions.CountByAgentAsync(agentId, ct) == (active is not null ? 1 : 0);
        var session = AgentSessionRecord.Create(agentId);
        await _sessions.CreateAsync(session, ct);

        var bootstrapMsg = session.FormatBootstrapMessage(agent.PersonalityFiles, isFirst);
        await _logs.AppendAsync(AgentLogRecord.System(agentId, bootstrapMsg), ct);

        return session;
    }

    public async Task<AgentSessionRecord?> EndAsync(Guid agentId, Guid ownerId, CancellationToken ct = default)
    {
        await EnsureAgentOwnedAsync(agentId, ownerId, ct);

        var active = await _sessions.GetByAsync(new AgentSessionFilter { AgentId = agentId, Status = SessionStatus.Active }, ct);
        if (active is null) return null;

        active.End();
        await _sessions.SaveChangesAsync(ct);
        return active;
    }

    private async Task EnsureAgentOwnedAsync(Guid agentId, Guid ownerId, CancellationToken ct)
    {
        _ = await GetOwnedAgentAsync(agentId, ownerId, ct);
    }

    private async Task<AgentRecord> GetOwnedAgentAsync(Guid agentId, Guid ownerId, CancellationToken ct)
    {
        var agent = await _agents.GetByAsync(new AgentFilter { Id = agentId, OwnerId = ownerId }, ct);
        if (agent is null)
            throw new InvalidOperationException("Agent not found.");
        return agent;
    }
}
