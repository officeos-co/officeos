namespace EnterpriseAgentOs.Database.Models;

public sealed class SystemEventEntity
{
    public Guid Id { get; set; }
    public string Severity { get; set; } = "error";
    public string Category { get; set; } = "system";
    public string Message { get; set; } = string.Empty;
    public string? DetailJson { get; set; }
    public string? SkillName { get; set; }
    public Guid? AgentId { get; set; }
    public string? CorrelationId { get; set; }
    public bool Acknowledged { get; set; }
    public DateTime CreatedAt { get; set; }
}
