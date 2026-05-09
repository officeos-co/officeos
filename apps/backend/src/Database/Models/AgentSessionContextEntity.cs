namespace EnterpriseAgentOs.Database.Models;

public sealed class AgentSessionContextEntity
{
    public Guid Id { get; set; }
    public Guid AgentId { get; set; }
    public string Summary { get; set; } = string.Empty;
    public Guid? LastCompactedLogId { get; set; }
    public DateTime? LastCompactedAt { get; set; }
    public int PreCompactTokens { get; set; }
    public int PostCompactTokens { get; set; }
    public int CompactionVersion { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
    public AgentEntity? Agent { get; set; }
}
