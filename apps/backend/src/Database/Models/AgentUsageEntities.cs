namespace OffceOs.Database.Models;

public sealed class AgentUsageCallEntity
{
    public Guid Id { get; set; }
    public Guid AgentId { get; set; }
    public Guid? WorkspaceId { get; set; }
    public Guid? OwnerId { get; set; }
    public Guid? RunId { get; set; }
    public Guid? ParentRunId { get; set; }
    public string CorrelationId { get; set; } = string.Empty;
    public DateTime Time { get; set; }
    public string Provider { get; set; } = string.Empty;
    public string Model { get; set; } = string.Empty;
    public int DurationMs { get; set; }
    public long InputTokens { get; set; }
    public long OutputTokens { get; set; }
    public long? CacheReadTokens { get; set; }
    public long? CacheWriteTokens { get; set; }
    public long? ReasoningTokens { get; set; }
    public bool EstimatedTokens { get; set; }
    public long Credits { get; set; }
    public string Activity { get; set; } = AgentUsageActivityKinds.General;
    public string Outcome { get; set; } = AgentUsageOutcomeKinds.Success;
    public AgentEntity? Agent { get; set; }
    public WorkspaceEntity? Workspace { get; set; }
    public UserEntity? Owner { get; set; }
    public AgentRunEntity? Run { get; set; }
    public List<AgentUsageContextPartEntity> ContextParts { get; set; } = [];
}

public sealed class AgentUsageContextPartEntity
{
    public Guid Id { get; set; }
    public Guid CallId { get; set; }
    public string Kind { get; set; } = AgentUsageContextPartKinds.Other;
    public string Label { get; set; } = string.Empty;
    public string? Role { get; set; }
    public string? Tool { get; set; }
    public string? Integration { get; set; }
    public long Tokens { get; set; }
    public bool EstimatedTokens { get; set; }
    public int CharacterCount { get; set; }
    public AgentUsageCallEntity? Call { get; set; }
}
