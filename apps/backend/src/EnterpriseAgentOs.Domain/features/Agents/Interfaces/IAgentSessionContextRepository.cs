namespace EnterpriseAgentOs.Domain.Features.Agents;

public sealed record AgentSessionContextFilter
{
    public Guid? AgentId { get; init; }
}

public interface IAgentSessionContextRepository
{
    Task<AgentSessionContextRecord?> GetByAsync(AgentSessionContextFilter filter, CancellationToken ct = default);
    Task UpsertAsync(AgentSessionContextRecord context, CancellationToken ct = default);
}
