namespace OffceOs.Database.Models;

public sealed class OrganizationProviderProfileEntity
{
    public Guid Id { get; set; }
    public Guid OrganizationId { get; set; }
    public string Provider { get; set; } = string.Empty;
    public string DisplayName { get; set; } = string.Empty;
    public string AllowedModelsJson { get; set; } = "[]";
    public string EncryptedApiKey { get; set; } = string.Empty;
    public bool Enabled { get; set; } = true;
    public DateTime ConfiguredAt { get; set; } = DateTime.UtcNow;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    public OrganizationEntity? Organization { get; set; }
}
