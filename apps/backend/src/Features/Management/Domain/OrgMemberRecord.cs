namespace OffceOs.Domain.Features.Management;

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
    public OrgRole Role { get; set; } = OrgRole.Member;
    public MemberStatus Status { get; set; } = MemberStatus.Invited;
    public DateTime CreatedAt { get; init; } = DateTime.UtcNow;

    // ── Factory methods ─────────────────────────────────────────────────────

    public static OrgMemberRecord Invite(Guid organizationId, string email, OrgRole role = OrgRole.Member)
    {
        if (role == OrgRole.Owner)
            throw new InvalidOperationException("Cannot invite as Owner — use CreateOwner instead.");

        return new OrgMemberRecord
        {
            OrganizationId = organizationId,
            Email = email.Trim().ToLowerInvariant(),
            Role = role,
            Status = MemberStatus.Invited,
        };
    }

    public static OrgMemberRecord CreateOwner(Guid organizationId, Guid userId, string email) => new()
    {
        OrganizationId = organizationId,
        UserId = userId,
        Email = email.Trim().ToLowerInvariant(),
        Role = OrgRole.Owner,
        Status = MemberStatus.Active,
    };
}
