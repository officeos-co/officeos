namespace OffceOs.Domain.Features.Context;

public sealed record IntegrationConnectionFilter
{
    public Guid? Id { get; init; }
    public IntegrationProviderType? Provider { get; init; }
    public Guid? CreatedById { get; init; }
    public Guid? WorkspaceId { get; init; }
}
