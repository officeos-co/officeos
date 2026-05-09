namespace OffceOs.Domain.Features.Management;

public sealed class WorkspaceOrganizationGrantRecord
{
    public Guid Id { get; init; } = Guid.NewGuid();
    public Guid WorkspaceId { get; init; }
    public Guid OrganizationId { get; init; }
    public WorkspaceRole MaxRole { get; set; } = WorkspaceRole.Viewer;
    public DateTime CreatedAt { get; init; } = DateTime.UtcNow;
}
