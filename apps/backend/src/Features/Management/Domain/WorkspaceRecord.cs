namespace OffceOs.Domain.Features.Management;

public sealed class WorkspaceRecord
{
    public Guid Id { get; init; } = Guid.NewGuid();
    public WorkspaceOwnerKind OwnerKind { get; init; } = WorkspaceOwnerKind.Personal;
    public Guid? OwnerUserId { get; init; }
    public Guid? OrganizationId { get; init; }
    public string Name { get; set; } = string.Empty;
    public bool IsDefault { get; init; }
    public DateTime CreatedAt { get; init; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    public WorkspaceRole? Role { get; init; }

    public static WorkspaceRecord CreatePersonal(Guid userId, string? name, bool isDefault = false) => new()
    {
        OwnerKind = WorkspaceOwnerKind.Personal,
        OwnerUserId = userId,
        IsDefault = isDefault,
        Name = NormalizeName(name),
    };

    public static WorkspaceRecord CreateOrganization(Guid organizationId, string? name, bool isDefault = false) => new()
    {
        OwnerKind = WorkspaceOwnerKind.Organization,
        OrganizationId = organizationId,
        IsDefault = isDefault,
        Name = NormalizeName(name),
    };

    public void Rename(string? name)
    {
        Name = NormalizeName(name);
        UpdatedAt = DateTime.UtcNow;
    }

    public static string NormalizeName(string? name)
        => string.IsNullOrWhiteSpace(name) ? "Default" : name.Trim();
}
