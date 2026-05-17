namespace OffceOs.Database.Models;

public sealed class AgentSessionEntity
{
    public Guid Id { get; set; }
    public Guid AgentId { get; set; }
    public Guid? OwnerId { get; set; }
    public Guid? WorkspaceId { get; set; }
    public string Source { get; set; } = "manual";
    public string Purpose { get; set; } = "manual";
    public string CorrelationId { get; set; } = string.Empty;
    public Guid? RoutineId { get; set; }
    public Guid? TriggerId { get; set; }
    public Guid? DefinitionId { get; set; }
    public string Input { get; set; } = string.Empty;
    public string? TriggerPayloadJson { get; set; }
    public string Status { get; set; } = "queued";
    public string? Error { get; set; }
    public DateTime LastActivityAt { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? StartedAt { get; set; }
    public DateTime? CompletedAt { get; set; }
    public AgentEntity? Agent { get; set; }
    public WorkspaceEntity? Workspace { get; set; }
    public AgentSessionRuntimeEntity? Runtime { get; set; }
    public AgentSessionRepositoryEntity? Repository { get; set; }
    public AgentSessionPullRequestEntity? PullRequest { get; set; }
}
