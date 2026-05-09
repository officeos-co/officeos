namespace EnterpriseAgentOs.Domain.Features.Context;

public sealed record IntegrationActivityFilter
{
    public Guid? ConnectionId { get; init; }
    public int Limit { get; init; } = 100;
}
