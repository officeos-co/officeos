
namespace OffceOs.Features.AgentRoutines.Domain;

public sealed class AgentRoutineRecord
{
    public Guid Id { get; init; } = Guid.NewGuid();
    public Guid AgentId { get; init; }
    public string Name { get; set; } = string.Empty;
    public string Prompt { get; set; } = string.Empty;
    public AgentRoutineRepositoryConfig? Repository { get; set; }
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
        string repository,
        IReadOnlyCollection<string> events,
        string? authRef,
        string mode,
        TimeSpan pollInterval,
        string? encryptedSecret)
    {
        var repositoryRef = GitHubRepositoryRecord.Parse(repository);
        return new AgentRoutineTriggerRecord
        {
            RoutineId = routineId,
            Kind = AgentRoutineTriggerKinds.GitHub,
            Name = name.Trim(),
            Enabled = true,
            ConfigJson = JsonSerializer.Serialize(new GitHubRoutineTriggerConfig
            {
                Repository = repositoryRef.FullName,
                RepositoryUrl = repositoryRef.Url,
                Owner = repositoryRef.Owner,
                Repo = repositoryRef.Name,
                Events = events.Select(e => e.Trim()).Where(e => e.Length > 0).Distinct(StringComparer.OrdinalIgnoreCase).ToList(),
                AuthRef = string.IsNullOrWhiteSpace(authRef) ? null : authRef.Trim().ToLowerInvariant(),
                Mode = GitHubRoutineTriggerModes.Normalize(mode),
                PollIntervalSeconds = Math.Max(15, (int)pollInterval.TotalSeconds),
            }),
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

public sealed record AgentRoutineRepositoryConfig(
    string FullName,
    string CloneUrl,
    string? BaseBranch,
    string CredentialRef);

public sealed record AgentRoutineWithAgentRecord(
    AgentRoutineRecord Routine,
    string AgentName);

public sealed class GitHubRoutineTriggerConfig
{
    public string Repository { get; init; } = string.Empty;
    public string RepositoryUrl { get; init; } = string.Empty;
    public string Owner { get; init; } = string.Empty;
    public string Repo { get; init; } = string.Empty;
    public IReadOnlyList<string> Events { get; init; } = [];
    public string? AuthRef { get; init; }
    public string Mode { get; init; } = GitHubRoutineTriggerModes.Poll;
    public int PollIntervalSeconds { get; init; } = 60;
}

public sealed record GitHubRepositoryRecord(string Owner, string Name, string FullName, string Url)
{
    private static readonly Regex HttpsRegex = new(@"^https://github\.com/(?<owner>[^/\s]+)/(?<repo>[^/\s]+?)(?:\.git)?/?$", RegexOptions.IgnoreCase | RegexOptions.Compiled);
    private static readonly Regex SshRegex = new(@"^git@github\.com:(?<owner>[^/\s]+)/(?<repo>[^/\s]+?)(?:\.git)?$", RegexOptions.IgnoreCase | RegexOptions.Compiled);
    private static readonly Regex FullNameRegex = new(@"^(?<owner>[^/\s]+)/(?<repo>[^/\s]+?)(?:\.git)?$", RegexOptions.IgnoreCase | RegexOptions.Compiled);

    public static GitHubRepositoryRecord Parse(string repository)
    {
        if (string.IsNullOrWhiteSpace(repository))
            throw new InvalidOperationException("GitHub routine trigger repo is required.");

        var trimmed = repository.Trim();
        var match = HttpsRegex.Match(trimmed);
        if (!match.Success)
            match = SshRegex.Match(trimmed);
        if (!match.Success)
            match = FullNameRegex.Match(trimmed);
        if (!match.Success)
            throw new InvalidOperationException($"GitHub routine trigger repo '{repository}' is not a supported GitHub repository URL.");

        var owner = match.Groups["owner"].Value.Trim();
        var repo = match.Groups["repo"].Value.Trim();
        return new GitHubRepositoryRecord(owner, repo, $"{owner}/{repo}", $"https://github.com/{owner}/{repo}.git");
    }
}

public static class GitHubRoutineTriggerModes
{
    public const string Poll = "poll";
    public const string Webhook = "webhook";

    public static string Normalize(string? mode)
    {
        if (string.IsNullOrWhiteSpace(mode))
            return Poll;

        var normalized = mode.Trim().ToLowerInvariant();
        if (normalized is Poll or Webhook)
            return normalized;

        throw new InvalidOperationException($"GitHub routine trigger mode '{mode}' is not supported.");
    }
}

public static class AgentRoutineTriggerKinds
{
    public const string Schedule = "schedule";
    public const string Api = "api";
    public const string GitHub = "github";
}
