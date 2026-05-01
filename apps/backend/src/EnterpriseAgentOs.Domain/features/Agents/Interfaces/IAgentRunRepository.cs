namespace EnterpriseAgentOs.Domain.Features.Agents;

public interface IAgentRunRepository
{
    Task<AgentRunRecord> CreateAsync(AgentRunRecord run, CancellationToken ct = default);
    Task<AgentRunRecord?> GetAsync(Guid runId, CancellationToken ct = default);
    Task<IReadOnlyList<AgentRunRecord>> ListForAgentAsync(Guid agentId, Guid? parentRunId = null, CancellationToken ct = default);
    Task UpdateAsync(AgentRunRecord run, CancellationToken ct = default);
}
