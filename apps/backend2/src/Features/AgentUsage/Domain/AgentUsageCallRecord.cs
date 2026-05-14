namespace OffceOs.Domain.Features.AgentUsage;

public sealed record AgentUsageCallRecord
{
    public Guid Id { get; init; } = Guid.NewGuid();
    public Guid AgentId { get; init; }
    public Guid? WorkspaceId { get; init; }
    public Guid? OwnerId { get; init; }
    public Guid? RunId { get; init; }
    public Guid? ParentRunId { get; init; }
    public string CorrelationId { get; init; } = string.Empty;
    public DateTime Time { get; init; } = DateTime.UtcNow;
    public string Provider { get; init; } = string.Empty;
    public string Model { get; init; } = string.Empty;
    public int DurationMs { get; init; }
    public long InputTokens { get; init; }
    public long OutputTokens { get; init; }
    public long? CacheReadTokens { get; init; }
    public long? CacheWriteTokens { get; init; }
    public long? ReasoningTokens { get; init; }
    public bool EstimatedTokens { get; init; }
    public long Credits { get; init; }
    public string Activity { get; init; } = AgentUsageActivityKinds.General;
    public string Outcome { get; init; } = AgentUsageOutcomeKinds.Success;
    public IReadOnlyList<AgentUsageContextPartRecord> ContextParts { get; init; } = [];

    public long TotalTokens => InputTokens + OutputTokens;
}
