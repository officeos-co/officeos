namespace OffceOs.Domain.Features.Management;

public sealed class OrganizationPolicyProfileRecord
{
    public Guid Id { get; init; } = Guid.NewGuid();
    public Guid OrganizationId { get; init; }
    public bool BrowserToolsEnabled { get; set; } = true;
    public bool NetworkToolsEnabled { get; set; } = true;
    public bool ShellToolsEnabled { get; set; } = true;
    public bool FileWriteToolsEnabled { get; set; } = true;
    public string AllowedToolsJson { get; set; } = "[]";
    public string DeniedToolsJson { get; set; } = "[]";
    public string AllowedIntegrationsJson { get; set; } = "[]";
    public string DeniedIntegrationsJson { get; set; } = "[]";
    public DateTime CreatedAt { get; init; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    public static OrganizationPolicyProfileRecord Default(Guid organizationId) => new()
    {
        OrganizationId = organizationId,
    };
}
