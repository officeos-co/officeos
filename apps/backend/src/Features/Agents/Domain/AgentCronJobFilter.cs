namespace OffceOs.Domain.Features.Agents;

public sealed record AgentCronJobFilter
{
    public Guid? Id { get; init; }
    public Guid? AgentId { get; init; }
    public bool? Enabled { get; init; }
}
