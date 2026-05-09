namespace OffceOs.Database.Models;

public sealed class WorkspaceOrganizationGrantEntity
{
    public Guid Id { get; set; }
    public Guid WorkspaceId { get; set; }
    public Guid OrganizationId { get; set; }
    public string MaxRole { get; set; } = "Viewer";
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public WorkspaceEntity? Workspace { get; set; }
    public OrganizationEntity? Organization { get; set; }
}
