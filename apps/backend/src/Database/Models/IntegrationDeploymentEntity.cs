namespace OffceOs.Database.Models;

public sealed class IntegrationDeploymentEntity
{
    public Guid Id { get; set; }
    public Guid WorkspaceId { get; set; }
    public string IntegrationName { get; set; } = string.Empty;
    public Guid CreatedById { get; set; }
    public bool Enabled { get; set; } = true;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    public WorkspaceEntity? Workspace { get; set; }
    public UserEntity? CreatedBy { get; set; }
}
