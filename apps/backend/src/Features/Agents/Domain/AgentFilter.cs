namespace EnterpriseAgentOs.Domain.Features.Agents;

public sealed record AgentFilter
{
    public Guid? Id { get; init; }
    public Guid? OwnerId { get; init; }
    public bool IncludeDeleted { get; init; }
}
