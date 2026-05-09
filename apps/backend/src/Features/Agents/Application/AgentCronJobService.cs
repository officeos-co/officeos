namespace OffceOs.Application.Features.Agents;

internal sealed class AgentCronJobService : IAgentCronJobService
{
    private readonly IAgentCronJobRepository _agentCronJobRepository;
    private readonly IAgentRepository _agentRepository;

    public AgentCronJobService(IAgentCronJobRepository cronJobs, IAgentRepository agents)
    {
        _agentCronJobRepository = cronJobs;
        _agentRepository = agents;
    }

    public Task<IReadOnlyList<AgentCronJobWithAgentRecord>> ListForOwnerAsync(Guid ownerId, CancellationToken ct = default) =>
        _agentCronJobRepository.ListForOwnerAsync(ownerId, ct);

    public Task<AgentCronJobWithAgentRecord?> GetForOwnerAsync(Guid id, Guid ownerId, CancellationToken ct = default) =>
        _agentCronJobRepository.GetForOwnerAsync(id, ownerId, ct);

    public async Task<IReadOnlyList<AgentCronJobRecord>> ListForAgentAsync(Guid agentId, Guid ownerId, CancellationToken ct = default)
    {
        await EnsureAgentOwnedAsync(agentId, ownerId, ct);
        return await _agentCronJobRepository.ListAsync(agentId, ct);
    }

    public async Task<AgentCronJobRecord> CreateAsync(CreateAgentCronJobRequest request, Guid ownerId, CancellationToken ct = default)
    {
        await EnsureAgentOwnedAsync(request.AgentId, ownerId, ct);
        return await _agentCronJobRepository.CreateAsync(request.AgentId, request.Name, request.Expression, request.Prompt, ct);
    }

    public async Task<bool> SetEnabledAsync(Guid id, Guid ownerId, bool enabled, CancellationToken ct = default)
    {
        var job = await _agentCronJobRepository.GetForOwnerAsync(id, ownerId, ct);
        if (job is null) return false;

        await _agentCronJobRepository.SetEnabledAsync(id, enabled, ct);
        return true;
    }

    public async Task<bool> DeleteAsync(Guid id, Guid ownerId, CancellationToken ct = default)
    {
        var job = await _agentCronJobRepository.GetForOwnerAsync(id, ownerId, ct);
        if (job is null) return false;

        return await _agentCronJobRepository.DeleteAsync(id, ct);
    }

    private async Task EnsureAgentOwnedAsync(Guid agentId, Guid ownerId, CancellationToken ct)
    {
        var agent = await _agentRepository.GetByAsync(new AgentFilter { Id = agentId, OwnerId = ownerId }, ct);
        if (agent is null)
            throw new InvalidOperationException("Agent not found.");
    }
}
