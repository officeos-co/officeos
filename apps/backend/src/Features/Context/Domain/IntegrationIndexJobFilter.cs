namespace OffceOs.Domain.Features.Context;

public sealed record IntegrationIndexJobFilter
{
    public Guid? ConnectionId { get; init; }
    public int Limit { get; init; } = 20;
}
