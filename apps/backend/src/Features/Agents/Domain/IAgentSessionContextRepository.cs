namespace OffceOs.Features.Agents.Domain;

public interface IAgentSessionContextRepository
{
    Task<AgentSessionContextRecord?> GetByAsync(AgentSessionContextFilter filter, CancellationToken ct = default);
    Task UpsertAsync(AgentSessionContextRecord context, CancellationToken ct = default);
}
