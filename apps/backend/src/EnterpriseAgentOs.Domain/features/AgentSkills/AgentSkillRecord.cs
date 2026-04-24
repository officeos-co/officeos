namespace EnterpriseAgentOs.Domain.Features.AgentSkills;

public sealed class AgentSkillRecord
{
    public Guid Id { get; init; } = Guid.NewGuid();

    public Guid AgentId { get; init; }

    [Required]
    public string SkillName { get; init; }

    public DateTimeOffset EnabledAt { get; init; } = DateTimeOffset.UtcNow;
}
