namespace OffceOs.Database.Models;

public sealed class OrganizationAuditLogEntity
{
    public Guid Id { get; set; }
    public Guid OrganizationId { get; set; }
    public Guid? ActorUserId { get; set; }
    public Guid? WorkspaceId { get; set; }
    public Guid? AgentId { get; set; }
    public string Action { get; set; } = string.Empty;
    public string ResourceType { get; set; } = string.Empty;
    public string? ResourceId { get; set; }
    public string Outcome { get; set; } = "success";
    public string? CorrelationId { get; set; }
    public string MetadataJson { get; set; } = "{}";
    public DateTime OccurredAt { get; set; } = DateTime.UtcNow;

    public OrganizationEntity? Organization { get; set; }
    public UserEntity? Actor { get; set; }
    public WorkspaceEntity? Workspace { get; set; }
    public AgentEntity? Agent { get; set; }
}
