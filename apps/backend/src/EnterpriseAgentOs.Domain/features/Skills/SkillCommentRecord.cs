namespace EnterpriseAgentOs.Domain.Features.Skills;

/// <summary>
/// Operator comment on a skill. Markdown body, soft-delete-free.
/// </summary>
public sealed class SkillCommentRecord
{
    public Guid Id { get; init; } = Guid.NewGuid();

    public Guid SkillId { get; init; }
    public SkillRecord? Skill { get; init; }

    public Guid UserId { get; init; }
    public UserRecord? User { get; init; }

    [Required]
    public string Body { get; set; } = string.Empty;

    public DateTime CreatedAt { get; init; } = DateTime.UtcNow;
    public DateTime? UpdatedAt { get; set; }
}
