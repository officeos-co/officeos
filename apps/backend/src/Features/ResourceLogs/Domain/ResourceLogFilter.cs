namespace OffceOs.Domain.Features.ResourceLogs;

public sealed record ResourceLogFilter
{
    public Guid? Id { get; init; }
    public string? ResourceKind { get; init; }
    public Guid? ResourceId { get; init; }
    public string? ResourceName { get; init; }
    public string? Severity { get; init; }
    public Guid? AgentId { get; init; }
    public IReadOnlyList<Guid>? AgentIds { get; init; }
    public Guid? OwnerId { get; init; }
    public Guid? WorkspaceId { get; init; }
    public Guid? ChannelConnectionId { get; init; }
    public string? CorrelationId { get; init; }
    public IReadOnlyList<string>? CorrelationIds { get; init; }
    public ResourceLogType? Type { get; init; }
    public IReadOnlyList<ResourceLogType>? Types { get; init; }
    public string? WorkStatus { get; init; }
    public string? WorkPurpose { get; init; }
    public Guid? DefinitionId { get; init; }
    public bool? HasWorkStatus { get; init; }
    public string? Search { get; init; }
    public string? AgentName { get; init; }
    public string? ContentStartsWith { get; init; }
    public DateTime? FromInclusive { get; init; }
    public DateTime? ToExclusive { get; init; }
    public DateTime? Before { get; init; }
}
