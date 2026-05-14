namespace OffceOs.Database.Models;

public sealed class IntegrationCredentialEntity
{
    public Guid Id { get; set; }
    public Guid OwnerId { get; set; }
    public Guid WorkspaceId { get; set; }
    public string IntegrationName { get; set; } = string.Empty;
    public string AuthKind { get; set; } = string.Empty;
    public string State { get; set; } = string.Empty;
    public string EncryptedSecretEnvelope { get; set; } = string.Empty;
    public string? PublicAuthMetadataJson { get; set; }
    public string? ScopesJson { get; set; }
    public DateTime? ExpiresAtUtc { get; set; }
    public DateTime? ValidatedAt { get; set; }
    public DateTime? LastUsedAt { get; set; }
    public DateTime? ArchivedAt { get; set; }
    public Guid? CreatedBy { get; set; }
    public DateTime ConfiguredAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    public WorkspaceEntity? Workspace { get; set; }
}
