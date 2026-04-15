namespace EnterpriseAgentOs.Api.Database.Models;

public class AgentToolCallRecord
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid AgentId { get; set; }
    public Guid? UserId { get; set; }
    public string SkillName { get; set; } = "";
    public string Action { get; set; } = "";
    public string ParamsJson { get; set; } = "{}";
    public string? ResultSummary { get; set; }
    public long DurationMs { get; set; }
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;
}
