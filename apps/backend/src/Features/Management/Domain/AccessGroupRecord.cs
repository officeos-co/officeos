namespace OffceOs.Domain.Features.Management;

public sealed class AccessGroupRecord
{
    public Guid Id { get; init; } = Guid.NewGuid();
    public Guid OrganizationId { get; init; }
    public string Name { get; set; } = string.Empty;
    public DateTime CreatedAt { get; init; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    public static AccessGroupRecord Create(Guid organizationId, string name) => new()
    {
        OrganizationId = organizationId,
        Name = NormalizeName(name),
    };

    public void Rename(string name)
    {
        Name = NormalizeName(name);
        UpdatedAt = DateTime.UtcNow;
    }

    private static string NormalizeName(string name)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new InvalidOperationException("Access group name is required.");

        return name.Trim();
    }
}
