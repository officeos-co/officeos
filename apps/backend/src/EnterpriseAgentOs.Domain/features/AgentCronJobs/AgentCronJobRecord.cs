namespace EnterpriseAgentOs.Domain.Features.AgentCronJobs;

public sealed class AgentCronJobRecord
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

    public static AgentCronJobRecord Create(Guid agentId, string name, string expression, string prompt)
    {
        return new AgentCronJobRecord
        {
            Id = Guid.NewGuid(),
            AgentId = agentId,
            Name = name,
            Expression = expression,
            Prompt = prompt,
            Enabled = true,
            CreatedAt = DateTime.UtcNow,
        };
    }

    public void SetEnabled(bool enabled) => Enabled = enabled;

    public void UpdateLastRun(DateTime ranAt)
    {
        LastRunAt = ranAt;
    }

    public void SetNextRun(DateTime? nextRun)
    {
        NextRunAt = nextRun;
    }
}
