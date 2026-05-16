namespace OffceOs.Features.Integrations.Domain;

public sealed record IntegrationDeploymentFilter
{
    public Guid? Id { get; init; }
    public Guid? WorkspaceId { get; init; }
    public string? IntegrationName { get; init; }
    public bool? Enabled { get; init; }
}
