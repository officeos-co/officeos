namespace OffceOs.Database.Models;

public sealed class AccessGroupMemberEntity
{
    public Guid Id { get; set; }
    public Guid AccessGroupId { get; set; }
    public Guid UserId { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public AccessGroupEntity? AccessGroup { get; set; }
    public UserEntity? User { get; set; }
}
