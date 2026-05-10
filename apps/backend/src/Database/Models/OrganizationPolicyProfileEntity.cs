namespace OffceOs.Database.Models;

public sealed class OrganizationPolicyProfileEntity
{
    public Guid Id { get; set; }
    public Guid OrganizationId { get; set; }
    public bool BrowserToolsEnabled { get; set; } = true;
    public bool NetworkToolsEnabled { get; set; } = true;
    public bool ShellToolsEnabled { get; set; } = true;
    public bool FileWriteToolsEnabled { get; set; } = true;
    public string AllowedToolsJson { get; set; } = "[]";
    public string DeniedToolsJson { get; set; } = "[]";
    public string AllowedIntegrationsJson { get; set; } = "[]";
    public string DeniedIntegrationsJson { get; set; } = "[]";
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    public OrganizationEntity? Organization { get; set; }
}
