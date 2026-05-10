namespace OffceOs.Domain.Features.Management;

public sealed class OrganizationProviderProfileRecord
{
    public Guid Id { get; init; } = Guid.NewGuid();
    public Guid OrganizationId { get; init; }
    public string Provider { get; init; } = string.Empty;
    public string DisplayName { get; set; } = string.Empty;
    public string AllowedModelsJson { get; set; } = "[]";
    public string EncryptedApiKey { get; set; } = string.Empty;
    public bool Enabled { get; set; } = true;
    public DateTime ConfiguredAt { get; set; } = DateTime.UtcNow;
    public DateTime CreatedAt { get; init; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
}
