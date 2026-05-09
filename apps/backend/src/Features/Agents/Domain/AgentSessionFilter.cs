namespace EnterpriseAgentOs.Domain.Features.Agents;

public sealed record AgentSessionFilter
{
    public Guid? Id { get; init; }
    public Guid? AgentId { get; init; }
    public SessionStatus? Status { get; init; }
}
