namespace OffceOs.Domain.Features.AgentRoutines;

public sealed class AgentRoutineRecord
{
    public Guid Id { get; init; } = Guid.NewGuid();
    public Guid AgentId { get; init; }
    public string Name { get; set; } = string.Empty;
    public string Prompt { get; set; } = string.Empty;
    public bool Enabled { get; set; }
    public DateTime? LastTriggeredAt { get; set; }
    public DateTime CreatedAt { get; init; } = DateTime.UtcNow;
    public List<AgentRoutineTriggerRecord> Triggers { get; init; } = [];

    public static AgentRoutineRecord Create(Guid agentId, string name, string prompt)
    {
        return new AgentRoutineRecord
        {
            AgentId = agentId,
            Name = name.Trim(),
            Prompt = prompt.Trim(),
            Enabled = true,
        };
    }

    public void SetEnabled(bool enabled) => Enabled = enabled;

    public void MarkTriggered(DateTime triggeredAt)
    {
        LastTriggeredAt = triggeredAt;
    }
}

public sealed class AgentRoutineTriggerRecord
{
    public Guid Id { get; init; } = Guid.NewGuid();
    public Guid RoutineId { get; init; }
    public string Kind { get; init; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public bool Enabled { get; set; }
    public string ConfigJson { get; set; } = "{}";
    public string? SecretHash { get; set; }
    public string? EncryptedSecret { get; set; }
    public DateTime? LastTriggeredAt { get; set; }
    public DateTime? NextRunAt { get; set; }
    public DateTime CreatedAt { get; init; } = DateTime.UtcNow;

    public static AgentRoutineTriggerRecord CreateSchedule(Guid routineId, string name, string expression, DateTime? nextRun)
    {
        var trigger = new AgentRoutineTriggerRecord
        {
            RoutineId = routineId,
            Kind = AgentRoutineTriggerKinds.Schedule,
            Name = name.Trim(),
            Enabled = true,
            ConfigJson = JsonSerializer.Serialize(new Dictionary<string, string> { ["expression"] = new CronExpression(expression).Value }),
        };

        trigger.SetNextRun(nextRun);
        return trigger;
    }

    public static AgentRoutineTriggerRecord CreateApi(Guid routineId, string name, string secretHash)
    {
        return new AgentRoutineTriggerRecord
        {
            RoutineId = routineId,
            Kind = AgentRoutineTriggerKinds.Api,
            Name = name.Trim(),
            Enabled = true,
            ConfigJson = "{}",
            SecretHash = secretHash,
        };
    }

    public static AgentRoutineTriggerRecord CreateGitHub(
        Guid routineId,
        string name,
        string owner,
        string repo,
        IReadOnlyCollection<string> events,
        string encryptedSecret)
    {
        return new AgentRoutineTriggerRecord
        {
            RoutineId = routineId,
            Kind = AgentRoutineTriggerKinds.GitHub,
            Name = name.Trim(),
            Enabled = true,
            ConfigJson = JsonSerializer.Serialize(new GitHubRoutineTriggerConfig(owner.Trim(), repo.Trim(), events.Select(e => e.Trim()).Where(e => e.Length > 0).Distinct(StringComparer.OrdinalIgnoreCase).ToList())),
            EncryptedSecret = encryptedSecret,
        };
    }

    public void MarkTriggered(DateTime triggeredAt)
    {
        LastTriggeredAt = triggeredAt;
    }

    public void SetNextRun(DateTime? nextRun)
    {
        NextRunAt = nextRun;
    }
}

public sealed record AgentRoutineWithAgentRecord(
    AgentRoutineRecord Routine,
    string AgentName);

public sealed record GitHubRoutineTriggerConfig(
    string Owner,
    string Repo,
    IReadOnlyList<string> Events);

public static class AgentRoutineTriggerKinds
{
    public const string Schedule = "schedule";
    public const string Api = "api";
    public const string GitHub = "github";
}
