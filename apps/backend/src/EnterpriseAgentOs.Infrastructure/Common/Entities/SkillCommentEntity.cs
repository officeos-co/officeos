namespace EnterpriseAgentOs.Infrastructure.Common.Entities;

public sealed class SkillCommentEntity
{
    public Guid Id { get; set; }
    public Guid SkillId { get; set; }
    public Guid UserId { get; set; }
    public string Body { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
    public SkillEntity? Skill { get; set; }
    public UserEntity? User { get; set; }
}
