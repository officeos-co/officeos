namespace EnterpriseAgentOs.Domain.Skills;

/// <summary>
/// Operator comment on a skill. Markdown body, soft-delete-free.
/// </summary>
public sealed class SkillCommentRecord
{
    public Guid Id { get; init; } = Guid.NewGuid();

    public Guid SkillId { get; set; }
    public SkillRecord? Skill { get; set; }

    public Guid UserId { get; set; }
    public UserRecord? User { get; set; }

    [Required]
    public string Body { get; set; } = string.Empty;

    public DateTime CreatedAt { get; init; } = DateTime.UtcNow;
    public DateTime? UpdatedAt { get; set; }
}
