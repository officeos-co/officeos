namespace EnterpriseAgentOs.Domain.Features.Agents;

public sealed record AgentMemoryFilter
{
    public Guid? AgentId { get; init; }
    public string? Key { get; init; }
}
