namespace EnterpriseAgentOs.Infrastructure.Common.Entities;

public sealed class SkillLikeEntity
{
    public Guid Id { get; set; }
    public Guid SkillId { get; set; }
    public Guid UserId { get; set; }
    public DateTime CreatedAt { get; set; }
    public SkillEntity? Skill { get; set; }
    public UserEntity? User { get; set; }
}
