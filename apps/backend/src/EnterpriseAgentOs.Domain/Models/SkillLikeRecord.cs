namespace EnterpriseAgentOs.Domain.Models;

/// <summary>
/// A single user's like on a skill. Unique on (UserId, SkillId). Aggregated count
/// is derived by <c>SELECT COUNT(*) WHERE SkillId = @id</c> — no denormalized counter.
/// </summary>
public sealed class SkillLikeRecord
{
    public Guid Id { get; init; } = Guid.NewGuid();

    public Guid SkillId { get; set; }
    public SkillRecord? Skill { get; set; }

    public Guid UserId { get; set; }
    public UserRecord? User { get; set; }

    public DateTime CreatedAt { get; init; } = DateTime.UtcNow;
}
