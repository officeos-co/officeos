namespace OffceOs.Domain.Features.Management;

public sealed record OrganizationAuditLogFilter
{
    public Guid OrganizationId { get; init; }
    public DateTime? From { get; init; }
    public DateTime? To { get; init; }
    public string? Action { get; init; }
    public Guid? ActorUserId { get; init; }
    public Guid? WorkspaceId { get; init; }
    public Guid? AgentId { get; init; }
    public string? Outcome { get; init; }
    public string? Search { get; init; }
    public int Limit { get; init; } = 100;
}
