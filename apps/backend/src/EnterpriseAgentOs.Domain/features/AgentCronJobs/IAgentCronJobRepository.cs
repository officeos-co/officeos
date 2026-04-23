namespace EnterpriseAgentOs.Domain.Features.AgentCronJobs;

public interface IAgentCronJobRepository
{
    Task<IReadOnlyList<AgentCronJobRecord>> ListAsync(Guid agentId, CancellationToken ct = default);
    Task<IReadOnlyList<AgentCronJobRecord>> ListAllEnabledAsync(CancellationToken ct = default);
    Task<AgentCronJobRecord?> GetAsync(Guid id, CancellationToken ct = default);
    Task<AgentCronJobRecord> CreateAsync(Guid agentId, string name, string expression, string prompt, CancellationToken ct = default);
    Task UpdateAsync(AgentCronJobRecord record, CancellationToken ct = default);
    Task SetEnabledAsync(Guid id, bool enabled, CancellationToken ct = default);
    Task<bool> DeleteAsync(Guid id, CancellationToken ct = default);
}
