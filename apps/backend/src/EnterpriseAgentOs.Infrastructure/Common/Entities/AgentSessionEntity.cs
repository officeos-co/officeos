namespace EnterpriseAgentOs.Infrastructure.Common.Entities;

public sealed class AgentSessionEntity
{
    public Guid Id { get; set; }
    public Guid AgentId { get; set; }
    public string Status { get; set; } = "active";
    public int MessageCount { get; set; }
    public DateTime LastActivityAt { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? EndedAt { get; set; }
    public AgentEntity? Agent { get; set; }
}
