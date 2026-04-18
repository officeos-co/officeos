namespace EnterpriseAgentOs.Domain.Models;

public sealed class SkillRegistryRecord
{
    public Guid Id { get; init; } = Guid.NewGuid();
    [Required, MaxLength(64)]
    public string Name { get; set; } = string.Empty;
    [MaxLength(32)]
    public string Version { get; set; } = "1.0.0";
    [MaxLength(256)]
    public string? NpmPackage { get; set; }
    [MaxLength(512)]
    public string? BundleUrl { get; set; }
    public string? ManifestJson { get; set; }
    [Required, MaxLength(16)]
    public string Status { get; set; } = "active";
    public Guid? PublishedById { get; set; }
    public DateTime CreatedAt { get; init; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    public UserRecord? PublishedBy { get; set; }
}
