namespace EnterpriseAgentOs.Domain.Features.Agents;

public sealed class AgentSkillRecord
{
    public Guid Id { get; init; } = Guid.NewGuid();

    public Guid AgentId { get; set; }

    [Required]
    public string SkillName { get; set; } = string.Empty;

    public DateTimeOffset EnabledAt { get; set; } = DateTimeOffset.UtcNow;
}
