namespace EnterpriseAgentOs.Domain.Features.Agents;

public sealed class AgentCronJobRecord
{
    public Guid Id { get; init; } = Guid.NewGuid();
    public Guid AgentId { get; init; }
    public string Name { get; set; } = string.Empty;
    public CronExpression Expression { get; set; }
    public string Prompt { get; set; } = string.Empty;
    public bool Enabled { get; set; }
    public DateTime? LastRunAt { get; set; }
    public DateTime? NextRunAt { get; set; }
    public DateTime CreatedAt { get; init; } = DateTime.UtcNow;

    public static AgentCronJobRecord Create(Guid agentId, string name, string expression, string prompt)
    {
        return new AgentCronJobRecord
        {
            AgentId = agentId,
            Name = name,
            Expression = new CronExpression(expression),
            Prompt = prompt,
            Enabled = true,
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

public sealed record AgentCronJobWithAgentRecord(
    AgentCronJobRecord Job,
    string AgentName);
