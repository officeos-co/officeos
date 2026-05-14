namespace OffceOs.Domain.Features.Providers;

public sealed record ProviderResourceRecord
{
    public Guid Id { get; init; } = Guid.NewGuid();
    public Guid WorkspaceId { get; init; }
    public string Name { get; init; } = string.Empty;
    public string Type { get; init; } = string.Empty;
    public string DisplayName { get; init; } = string.Empty;
    public bool Enabled { get; init; } = true;
    public string? DefaultModel { get; init; }
    public IReadOnlyList<string> Models { get; init; } = [];
    public string AuthKind { get; init; } = ProviderAuthKind.ApiKey.ToStorageString();
    public string EncryptedCredentialsJson { get; init; } = string.Empty;
    public DateTime CreatedAt { get; init; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; init; } = DateTime.UtcNow;
}
