namespace OffceOs.Domain.Features.AgentUsage;

public sealed record AgentUsageFilter
{
    public Guid? OwnerId { get; init; }
    public Guid? WorkspaceId { get; init; }
    public Guid? AgentId { get; init; }
    public Guid? RunId { get; init; }
    public string? CorrelationId { get; init; }
    public string? Provider { get; init; }
    public string? Model { get; init; }
    public DateTime? FromInclusive { get; init; }
    public DateTime? ToExclusive { get; init; }
}
