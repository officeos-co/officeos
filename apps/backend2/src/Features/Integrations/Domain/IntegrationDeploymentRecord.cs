namespace OffceOs.Domain.Features.Integrations;

public sealed class IntegrationDeploymentRecord
{
    public Guid Id { get; init; } = Guid.NewGuid();
    public Guid OrganizationId { get; init; }
    public Guid WorkspaceId { get; init; }
    public string IntegrationName { get; init; } = string.Empty;
    public Guid CreatedById { get; init; }
    public bool Enabled { get; set; } = true;
    public DateTime CreatedAt { get; init; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
}
