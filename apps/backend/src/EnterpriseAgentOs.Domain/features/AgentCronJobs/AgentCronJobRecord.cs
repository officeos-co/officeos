namespace EnterpriseAgentOs.Domain.Features.AgentCronJobs;

public sealed class AgentCronJobRecord
{
    public Guid Id { get; private set; }
    public Guid AgentId { get; private set; }
    public string Name { get; private set; } = string.Empty;
    public string Expression { get; private set; } = string.Empty;
    public string Prompt { get; private set; } = string.Empty;
    public bool Enabled { get; private set; }
    public DateTime? LastRunAt { get; private set; }
    public DateTime? NextRunAt { get; private set; }
    public DateTime CreatedAt { get; private set; }

    private AgentCronJobRecord() { }

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
