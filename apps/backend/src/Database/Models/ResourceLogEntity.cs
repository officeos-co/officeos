using OffceOs.Domain.Features.ResourceLogs;

namespace OffceOs.Database.Models;

public sealed class ResourceLogEntity
{
    public Guid Id { get; set; }
    public string ResourceKind { get; set; } = ResourceLogKinds.Agent;
    public Guid? ResourceId { get; set; }
    public string? ResourceName { get; set; }
    public string? ParentResourceKind { get; set; }
    public Guid? ParentResourceId { get; set; }
    public Guid? AgentId { get; set; }
    public Guid? WorkspaceId { get; set; }
    public DateTime Time { get; set; }
    public ResourceLogType Type { get; set; }
    public string Severity { get; set; } = ResourceLogSeverityKinds.Info;
    public string? Tool { get; set; }
    public string? Integration { get; set; }
    public string? Channel { get; set; }
    public Guid? ChannelConnectionId { get; set; }
    public string Content { get; set; } = string.Empty;
    public string? MetadataJson { get; set; }
    public int? DurationMs { get; set; }
    public int? InputTokens { get; set; }
    public int? OutputTokens { get; set; }
    public string? CorrelationId { get; set; }
    public string? WorkStatus { get; set; }
    public string? WorkPurpose { get; set; }
    public Guid? DefinitionId { get; set; }
    public DateTime? StartedAt { get; set; }
    public DateTime? CompletedAt { get; set; }
    public string? WorkError { get; set; }
    public AgentEntity? Agent { get; set; }
    public WorkspaceEntity? Workspace { get; set; }
}
