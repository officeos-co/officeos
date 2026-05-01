namespace EnterpriseAgentOs.Domain.Features.Agents;

public interface IAgentSessionContextRepository
{
    Task<AgentSessionContextRecord?> GetAsync(Guid agentId, CancellationToken ct = default);
    Task UpsertAsync(AgentSessionContextRecord context, CancellationToken ct = default);
}
