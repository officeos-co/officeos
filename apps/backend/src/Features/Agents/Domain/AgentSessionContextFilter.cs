namespace OffceOs.Domain.Features.Agents;

public sealed record AgentSessionContextFilter
{
    public Guid? AgentId { get; init; }
}
