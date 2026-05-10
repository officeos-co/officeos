namespace OffceOs.Database.Models;

public sealed class AccessGroupWorkspaceGrantEntity
{
    public Guid Id { get; set; }
    public Guid AccessGroupId { get; set; }
    public Guid WorkspaceId { get; set; }
    public string Role { get; set; } = "Viewer";
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    public AccessGroupEntity? AccessGroup { get; set; }
    public WorkspaceEntity? Workspace { get; set; }
}
