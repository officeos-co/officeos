namespace EnterpriseAgentOs.Application.Features.Agents;

internal sealed class AgentCronJobService : IAgentCronJobService
{
    private readonly IAgentCronJobRepository _cronJobs;
    private readonly IAgentRepository _agents;

    public AgentCronJobService(IAgentCronJobRepository cronJobs, IAgentRepository agents)
    {
        _cronJobs = cronJobs;
        _agents = agents;
    }

    public Task<IReadOnlyList<AgentCronJobWithAgentRecord>> ListForOwnerAsync(Guid ownerId, CancellationToken ct = default) =>
        _cronJobs.ListForOwnerAsync(ownerId, ct);

    public Task<AgentCronJobWithAgentRecord?> GetForOwnerAsync(Guid id, Guid ownerId, CancellationToken ct = default) =>
        _cronJobs.GetForOwnerAsync(id, ownerId, ct);

    public async Task<IReadOnlyList<AgentCronJobRecord>> ListForAgentAsync(Guid agentId, Guid ownerId, CancellationToken ct = default)
    {
        await EnsureAgentOwnedAsync(agentId, ownerId, ct);
        return await _cronJobs.ListAsync(agentId, ct);
    }

    public async Task<AgentCronJobRecord> CreateAsync(CreateAgentCronJobRequest request, Guid ownerId, CancellationToken ct = default)
    {
        await EnsureAgentOwnedAsync(request.AgentId, ownerId, ct);
        return await _cronJobs.CreateAsync(request.AgentId, request.Name, request.Expression, request.Prompt, ct);
    }

    public async Task<bool> SetEnabledAsync(Guid id, Guid ownerId, bool enabled, CancellationToken ct = default)
    {
        var job = await _cronJobs.GetForOwnerAsync(id, ownerId, ct);
        if (job is null) return false;

        await _cronJobs.SetEnabledAsync(id, enabled, ct);
        return true;
    }

    public async Task<bool> DeleteAsync(Guid id, Guid ownerId, CancellationToken ct = default)
    {
        var job = await _cronJobs.GetForOwnerAsync(id, ownerId, ct);
        if (job is null) return false;

        return await _cronJobs.DeleteAsync(id, ct);
    }

    private async Task EnsureAgentOwnedAsync(Guid agentId, Guid ownerId, CancellationToken ct)
    {
        var agent = await _agents.GetByAsync(new AgentFilter { Id = agentId, OwnerId = ownerId }, ct);
        if (agent is null)
            throw new InvalidOperationException("Agent not found.");
    }
}
