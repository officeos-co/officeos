namespace OffceOs.Features.Browser.Domain;

public sealed class BrowserResourceRecord
{
    public Guid Id { get; init; } = Guid.NewGuid();
    public Guid OwnerId { get; init; }
    public Guid WorkspaceId { get; init; }
    public string DisplayName { get; set; } = string.Empty;
    public Guid? CurrentAgentId { get; set; }
    public DateTime CreatedAt { get; init; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    public static BrowserResourceRecord Create(Guid ownerId, Guid workspaceId, string displayName) => new()
    {
        OwnerId = ownerId,
        WorkspaceId = workspaceId,
        DisplayName = NormalizeName(displayName, "Browser"),
    };

    internal static string NormalizeName(string value, string fallback)
        => string.IsNullOrWhiteSpace(value) ? fallback : value.Trim();
}
