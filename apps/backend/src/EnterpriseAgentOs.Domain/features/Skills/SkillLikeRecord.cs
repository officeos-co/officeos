namespace EnterpriseAgentOs.Domain.Features.Skills;

/// <summary>
/// A single user's like on a skill. Unique on (UserId, SkillId). Aggregated count
/// is derived by <c>SELECT COUNT(*) WHERE SkillId = @id</c> — no denormalized counter.
/// </summary>
public sealed class SkillLikeRecord
{
    public Guid Id { get; init; } = Guid.NewGuid();

    public Guid SkillId { get; init; }
    public SkillRecord? Skill { get; init; }

    public Guid UserId { get; init; }
    public UserRecord? User { get; init; }

    public DateTime CreatedAt { get; init; } = DateTime.UtcNow;
}
