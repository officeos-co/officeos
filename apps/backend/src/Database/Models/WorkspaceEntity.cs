namespace OffceOs.Database.Models;

public sealed class WorkspaceEntity
{
    public Guid Id { get; set; }
    public Guid? OwnerUserId { get; set; }
    public string OwnerKind { get; set; } = "personal";
    public string Name { get; set; } = string.Empty;
    public bool IsDefault { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    public UserEntity? OwnerUser { get; set; }
}
