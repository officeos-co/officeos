namespace OffceOs.Database.Models;

public sealed class OrgMemberEntity
{
    public Guid Id { get; set; }
    public Guid OrganizationId { get; set; }
    public Guid? UserId { get; set; }
    public string Email { get; set; } = string.Empty;
    public string Role { get; set; } = "Member";
    public string Status { get; set; } = "invited";
    public DateTime CreatedAt { get; set; }
}
