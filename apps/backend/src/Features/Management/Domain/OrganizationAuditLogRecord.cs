namespace OffceOs.Domain.Features.Management;

public sealed record OrganizationAuditLogRecord
{
    public Guid Id { get; init; } = Guid.NewGuid();
    public Guid OrganizationId { get; init; }
    public Guid? ActorUserId { get; init; }
    public Guid? WorkspaceId { get; init; }
    public Guid? AgentId { get; init; }
    public string Action { get; init; } = string.Empty;
    public string ResourceType { get; init; } = string.Empty;
    public string? ResourceId { get; init; }
    public string Outcome { get; init; } = OrganizationAuditKinds.Success;
    public string? CorrelationId { get; init; }
    public string MetadataJson { get; init; } = "{}";
    public DateTime OccurredAt { get; init; } = DateTime.UtcNow;
}
