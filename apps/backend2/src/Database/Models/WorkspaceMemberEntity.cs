namespace OffceOs.Database.Models;

public sealed class WorkspaceMemberEntity
{
    public Guid Id { get; set; }
    public Guid WorkspaceId { get; set; }
    public Guid UserId { get; set; }
    public string Role { get; set; } = "Viewer";
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public WorkspaceEntity? Workspace { get; set; }
    public UserEntity? User { get; set; }
}
