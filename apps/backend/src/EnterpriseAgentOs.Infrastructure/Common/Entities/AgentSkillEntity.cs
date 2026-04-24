namespace EnterpriseAgentOs.Infrastructure.Common.Entities;

public sealed class AgentSkillEntity
{
    public Guid Id { get; set; }
    public Guid AgentId { get; set; }
    public string SkillName { get; set; } = string.Empty;
    public DateTimeOffset EnabledAt { get; set; }
}
