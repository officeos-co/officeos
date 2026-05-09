namespace EnterpriseAgentOs.Database.Models;

public sealed class AgentCronJobEntity
{
    public Guid Id { get; set; }
    public Guid AgentId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Expression { get; set; } = string.Empty;
    public string Prompt { get; set; } = string.Empty;
    public bool Enabled { get; set; }
    public DateTime? LastRunAt { get; set; }
    public DateTime? NextRunAt { get; set; }
    public DateTime CreatedAt { get; set; }
}
