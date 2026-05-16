namespace OffceOs.Features.Providers.Domain;

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
    public string Phase { get; init; } = ProviderResourcePhaseKinds.Pending;
    public string StatusMessage { get; init; } = string.Empty;
    public string? Account { get; init; }
    public DateTime? ExpiresAt { get; init; }
    public DateTime? LastValidatedAt { get; init; }
    public DateTime CreatedAt { get; init; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; init; } = DateTime.UtcNow;
}

public static class ProviderResourcePhaseKinds
{
    public const string Pending = "Pending";
    public const string Ready = "Ready";
    public const string Error = "Error";
}
