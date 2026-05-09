namespace EnterpriseAgentOs.Application.Features.Agents;

public interface IAgentCronJobService
{
    Task<IReadOnlyList<AgentCronJobWithAgentRecord>> ListForOwnerAsync(Guid ownerId, CancellationToken ct = default);
    Task<AgentCronJobWithAgentRecord?> GetForOwnerAsync(Guid id, Guid ownerId, CancellationToken ct = default);
    Task<IReadOnlyList<AgentCronJobRecord>> ListForAgentAsync(Guid agentId, Guid ownerId, CancellationToken ct = default);
    Task<AgentCronJobRecord> CreateAsync(CreateAgentCronJobRequest request, Guid ownerId, CancellationToken ct = default);
    Task<bool> SetEnabledAsync(Guid id, Guid ownerId, bool enabled, CancellationToken ct = default);
    Task<bool> DeleteAsync(Guid id, Guid ownerId, CancellationToken ct = default);
}

public sealed record CreateAgentCronJobRequest(
    Guid AgentId,
    string Name,
    string Expression,
    string Prompt);
