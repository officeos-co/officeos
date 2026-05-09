namespace OffceOs.Domain.Features.Management;

public sealed class WorkspaceRecord
{
    public Guid Id { get; init; } = Guid.NewGuid();
    public Guid UserId { get; init; }
    public string Name { get; set; } = string.Empty;
    public DateTime CreatedAt { get; init; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    public static WorkspaceRecord Create(Guid userId, string? name) => new()
    {
        UserId = userId,
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
