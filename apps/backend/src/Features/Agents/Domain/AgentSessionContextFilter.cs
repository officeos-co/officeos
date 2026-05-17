namespace OffceOs.Features.Agents.Domain;

public sealed record AgentSessionContextFilter
{
    public Guid? AgentId { get; init; }
    public Guid? SessionId { get; init; }
}
