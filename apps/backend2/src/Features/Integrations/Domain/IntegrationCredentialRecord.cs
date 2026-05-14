namespace OffceOs.Domain.Features.Integrations;

public sealed class IntegrationCredentialRecord
{
    public Guid Id { get; init; } = Guid.NewGuid();
    public Guid OwnerId { get; init; }
    public Guid WorkspaceId { get; init; }
    public string IntegrationName { get; init; } = string.Empty;
    public string AuthKind { get; init; } = IntegrationCredentialAuthKinds.ApiKey;
    public IntegrationCredentialState State { get; init; } = IntegrationCredentialState.Active;
    public string EncryptedSecretEnvelope { get; init; } = string.Empty;
    public string? PublicAuthMetadataJson { get; init; }
    public string? ScopesJson { get; init; }
    public DateTime? ExpiresAtUtc { get; init; }
    public DateTime? ValidatedAt { get; init; }
    public DateTime? LastUsedAt { get; init; }
    public DateTime? ArchivedAt { get; init; }
    public Guid? CreatedBy { get; init; }
    public DateTime ConfiguredAt { get; init; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; init; } = DateTime.UtcNow;
}
