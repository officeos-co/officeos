namespace EnterpriseAgentOs.Domain.Features.Organizations;

public sealed class OrgMemberRecord
{
    public Guid Id { get; init; } = Guid.NewGuid();
    public Guid OrganizationId { get; init; }
    /// <summary>
    /// FK → UserRecord.Id. Null when the invite is pending (user has not signed up yet).
    /// </summary>
    public Guid? UserId { get; set; }
    [Required, MaxLength(256)]
    public string Email { get; init; } = string.Empty;
    /// <summary>"Owner" | "Admin" | "Member"</summary>
    [Required, MaxLength(16)]
    public string Role { get; set; } = "Member";
    /// <summary>"active" | "invited"</summary>
    [Required, MaxLength(16)]
    public string Status { get; set; } = "invited";
    public DateTime CreatedAt { get; init; } = DateTime.UtcNow;
}
