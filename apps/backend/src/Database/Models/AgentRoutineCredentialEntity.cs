namespace OffceOs.Database.Models;

public sealed class AgentRoutineCredentialEntity
{
    public Guid Id { get; set; }
    public Guid OwnerId { get; set; }
    public Guid WorkspaceId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Provider { get; set; } = string.Empty;
    public string AuthKind { get; set; } = string.Empty;
    public string EncryptedSecret { get; set; } = string.Empty;
    public string? PublicMetadataJson { get; set; }
    public string? ScopesJson { get; set; }
    public DateTime? ExpiresAtUtc { get; set; }
    public DateTime? LastUsedAt { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
    public UserEntity? Owner { get; set; }
    public WorkspaceEntity? Workspace { get; set; }
}
