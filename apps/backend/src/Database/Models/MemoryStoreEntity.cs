namespace OffceOs.Database.Models;

public sealed class MemoryStoreEntity
{
    public Guid Id { get; set; }
    public Guid OwnerId { get; set; }
    public Guid WorkspaceId { get; set; }
    public string DisplayName { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
    public UserEntity? Owner { get; set; }
    public WorkspaceEntity? Workspace { get; set; }
}
