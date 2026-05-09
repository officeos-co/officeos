namespace OffceOs.Domain.Features.Context;

public sealed record IntegrationConnectionFilter
{
    public Guid? Id { get; init; }
    public IntegrationProviderType? Provider { get; init; }
}
