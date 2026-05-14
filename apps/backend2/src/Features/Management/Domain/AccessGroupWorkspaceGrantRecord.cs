namespace OffceOs.Domain.Features.Management;

public sealed class AccessGroupWorkspaceGrantRecord
{
    public Guid Id { get; init; } = Guid.NewGuid();
    public Guid AccessGroupId { get; init; }
    public Guid WorkspaceId { get; init; }
    public WorkspaceRole Role { get; set; } = WorkspaceRole.Viewer;
    public DateTime CreatedAt { get; init; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
}
