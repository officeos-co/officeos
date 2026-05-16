using OffceOs.Features.ResourceLogs.Application;
using OffceOs.Features.Agents.Domain;
using OffceOs.Features.ResourceLogs.Domain;
namespace OffceOs.Features.Agents.Application;

internal sealed class AgentSessionService : IAgentSessionService
{
    private readonly IAgentRepository _agentRepository;
    private readonly IAgentSessionRepository _agentSessionRepository;
    private readonly IResourceLogService _resourceLogService;

    public AgentSessionService(
        IAgentRepository agents,
        IAgentSessionRepository sessions,
        IResourceLogService logs)
    {
        _agentRepository = agents;
        _agentSessionRepository = sessions;
        _resourceLogService = logs;
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

    public async Task<AgentSessionRecord?> GetActiveAsync(Guid agentId, Guid ownerId, CancellationToken ct = default)
    {
        await EnsureAgentOwnedAsync(agentId, ownerId, ct);
        return await _agentSessionRepository.GetByAsync(new AgentSessionFilter { AgentId = agentId, Status = SessionStatus.Active }, ct);
    }

    public async Task<AgentSessionRecord> CreateAsync(Guid agentId, Guid ownerId, CancellationToken ct = default)
    {
        var agent = await GetOwnedAgentAsync(agentId, ownerId, ct);

        var active = await _agentSessionRepository.GetByAsync(new AgentSessionFilter { AgentId = agentId, Status = SessionStatus.Active }, ct);
        if (active is not null)
        {
            active.End();
            await _agentSessionRepository.SaveChangesAsync(ct);
        }

        var isFirst = await _agentSessionRepository.CountByAgentAsync(agentId, ct) == (active is not null ? 1 : 0);
        var session = AgentSessionRecord.Create(agentId);
        await _agentSessionRepository.CreateAsync(session, ct);

        var bootstrapMsg = session.FormatBootstrapMessage(agent.PersonalityFiles, isFirst);
        await _resourceLogService.AppendAsync(ResourceLogRecord.System(agentId, bootstrapMsg), ct);

        return session;
    }

    public async Task<AgentSessionRecord?> EndAsync(Guid agentId, Guid ownerId, CancellationToken ct = default)
    {
        await EnsureAgentOwnedAsync(agentId, ownerId, ct);

        var active = await _agentSessionRepository.GetByAsync(new AgentSessionFilter { AgentId = agentId, Status = SessionStatus.Active }, ct);
        if (active is null) return null;

        active.End();
        await _agentSessionRepository.SaveChangesAsync(ct);
        return active;
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
}
