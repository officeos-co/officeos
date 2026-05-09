namespace EnterpriseAgentOs.Domain.Features.Agents;

public interface IAgentSessionContextRepository
{
    Task<AgentSessionContextRecord?> GetByAsync(AgentSessionContextFilter filter, CancellationToken ct = default);
    Task UpsertAsync(AgentSessionContextRecord context, CancellationToken ct = default);
}
