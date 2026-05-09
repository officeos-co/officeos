namespace EnterpriseAgentOs.Database.Models;

public sealed class AgentPersonalityEntity
{
    public Guid Id { get; set; }
    public Guid AgentId { get; set; }
    public string FileName { get; set; } = null!;
    public string Content { get; set; } = null!;
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
    public AgentEntity? Agent { get; set; }
}
