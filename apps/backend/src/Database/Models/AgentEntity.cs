namespace OffceOs.Database.Models;

public sealed class AgentEntity
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Provider { get; set; } = string.Empty;
    public string? Model { get; set; }
    public string Status { get; set; } = "pending";
    public string? PodName { get; set; }
    public string? ServiceUrl { get; set; }
    public string? Prompt { get; set; }
    public DateTime CreatedAt { get; set; }
    public bool IsDeleted { get; set; }
    public Guid? OwnerId { get; set; }
    public Guid? WorkspaceId { get; set; }
    public string? EncryptedBackendToken { get; set; }
    public UserEntity? Owner { get; set; }
    public WorkspaceEntity? Workspace { get; set; }
}
