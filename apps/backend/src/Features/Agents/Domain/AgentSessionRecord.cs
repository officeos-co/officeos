
namespace OffceOs.Features.Agents.Domain;

public sealed class AgentSessionRecord
{
    public Guid Id { get; init; } = Guid.NewGuid();
    public Guid AgentId { get; init; }
    public Guid? OwnerId { get; init; }
    public Guid? WorkspaceId { get; init; }

    public string Source { get; init; } = AgentSessionSourceKinds.Manual;
    public string Purpose { get; init; } = AgentWorkPurposeKinds.Manual;
    public string CorrelationId { get; init; } = Guid.NewGuid().ToString("N");
    public Guid? RoutineId { get; init; }
    public Guid? TriggerId { get; init; }
    public Guid? DefinitionId { get; init; }
    public string Input { get; init; } = string.Empty;
    public string? TriggerPayloadJson { get; init; }

    public SessionStatus Status { get; set; } = SessionStatus.Queued;
    public string? Error { get; set; }
    public DateTime LastActivityAt { get; set; } = DateTime.UtcNow;
    public DateTime CreatedAt { get; init; } = DateTime.UtcNow;
    public DateTime? StartedAt { get; set; }
    public DateTime? CompletedAt { get; set; }

    public string? SandboxId { get; set; }
    public string? ServiceUrl { get; set; }

    public string? RepositoryFullName { get; init; }
    public string? RepositoryCloneUrl { get; init; }
    public string? RepositoryBaseBranch { get; set; }
    public string? RepositoryCredentialRef { get; init; }
    public string? RepositoryBranch { get; set; }
    public string? PullRequestUrl { get; set; }
    public int? PullRequestNumber { get; set; }
    public string? CommitSha { get; set; }

    public AgentRecord? Agent { get; init; }

    public bool IsTerminal => Status is SessionStatus.Completed or SessionStatus.Failed or SessionStatus.Canceled;
    public bool HasRepository => !string.IsNullOrWhiteSpace(RepositoryCloneUrl) || !string.IsNullOrWhiteSpace(RepositoryFullName);

    public static AgentSessionRecord CreateRun(
        AgentRecord agent,
        string input,
        string purpose,
        string source,
        string correlationId,
        Guid? routineId = null,
        Guid? triggerId = null,
        Guid? definitionId = null,
        string? triggerPayloadJson = null,
        AgentSessionRepositoryConfig? repository = null) => new()
    {
        AgentId = agent.Id,
        OwnerId = agent.OwnerId,
        WorkspaceId = agent.WorkspaceId,
        Input = input,
        Purpose = AgentWorkPurposeKinds.Normalize(purpose),
        Source = AgentSessionSourceKinds.Normalize(source),
        CorrelationId = string.IsNullOrWhiteSpace(correlationId) ? Guid.NewGuid().ToString("N") : correlationId,
        RoutineId = routineId,
        TriggerId = triggerId,
        DefinitionId = definitionId,
        TriggerPayloadJson = string.IsNullOrWhiteSpace(triggerPayloadJson) ? null : triggerPayloadJson,
        RepositoryFullName = repository?.FullName,
        RepositoryCloneUrl = repository?.CloneUrl,
        RepositoryBaseBranch = repository?.BaseBranch,
        RepositoryCredentialRef = repository?.CredentialRef,
    };

    public static AgentSessionRecord Create(Guid agentId) => new()
    {
        AgentId = agentId,
        Source = AgentSessionSourceKinds.Manual,
        Purpose = AgentWorkPurposeKinds.Manual,
        Input = "Resource attachment session.",
        Status = SessionStatus.Queued,
    };

    public void MarkRunning(string sandboxId, string serviceUrl, DateTime now)
    {
        Status = SessionStatus.Running;
        SandboxId = sandboxId;
        ServiceUrl = serviceUrl;
        StartedAt ??= now;
        RecordActivity(now);
    }

    public void MarkCompleted(DateTime now)
    {
        Status = SessionStatus.Completed;
        CompletedAt = now;
        RecordActivity(now);
    }

    public void MarkFailed(string error, DateTime now)
    {
        Status = SessionStatus.Failed;
        Error = error;
        CompletedAt = now;
        RecordActivity(now);
    }

    public void RecordGitHubArtifact(string branch, string? commitSha, string pullRequestUrl, int? pullRequestNumber)
    {
        RepositoryBranch = branch;
        CommitSha = commitSha;
        PullRequestUrl = pullRequestUrl;
        PullRequestNumber = pullRequestNumber;
    }

    public void RecordActivity(DateTime at)
    {
        LastActivityAt = at;
    }
}

public sealed record AgentSessionRepositoryConfig(
    string FullName,
    string CloneUrl,
    string? BaseBranch,
    string? CredentialRef);
