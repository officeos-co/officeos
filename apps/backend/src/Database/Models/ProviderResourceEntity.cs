namespace OffceOs.Database.Models;

public sealed class ProviderResourceEntity
{
    public Guid Id { get; set; }
    public Guid WorkspaceId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Type { get; set; } = string.Empty;
    public string DisplayName { get; set; } = string.Empty;
    public bool Enabled { get; set; } = true;
    public string? DefaultModel { get; set; }
    public string AllowedModelsJson { get; set; } = "[]";
    public string AuthKind { get; set; } = "api_key";
    public string EncryptedCredentialsJson { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    public WorkspaceEntity? Workspace { get; set; }
}
