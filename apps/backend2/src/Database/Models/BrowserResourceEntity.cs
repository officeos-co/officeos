namespace OffceOs.Database.Models;

public sealed class BrowserResourceEntity
{
    public Guid Id { get; set; }
    public Guid OwnerId { get; set; }
    public Guid WorkspaceId { get; set; }
    public string DisplayName { get; set; } = string.Empty;
    public Guid? CurrentAgentId { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
    public UserEntity? Owner { get; set; }
    public WorkspaceEntity? Workspace { get; set; }
    public AgentEntity? CurrentAgent { get; set; }
}
