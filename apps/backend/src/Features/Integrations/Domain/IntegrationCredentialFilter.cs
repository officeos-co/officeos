namespace OffceOs.Domain.Features.Integrations;

public sealed record IntegrationCredentialFilter
{
    public Guid? Id { get; init; }
    public Guid? OwnerId { get; init; }
    public Guid? WorkspaceId { get; init; }
    public string? IntegrationName { get; init; }
    public bool IncludeArchived { get; init; }
}
