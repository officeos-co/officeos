namespace OffceOs.Features.Context.Domain;

public sealed class MemoryStoreRecord
{
    public Guid Id { get; init; } = Guid.NewGuid();
    public Guid OwnerId { get; init; }
    public Guid WorkspaceId { get; init; }
    public string DisplayName { get; set; } = string.Empty;
    public DateTime CreatedAt { get; init; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    public static MemoryStoreRecord Create(Guid ownerId, Guid workspaceId, string displayName) => new()
    {
        OwnerId = ownerId,
        WorkspaceId = workspaceId,
        DisplayName = string.IsNullOrWhiteSpace(displayName) ? "Memory Store" : displayName.Trim(),
    };
}

public sealed class MemoryStoreEntryRecord
{
    public Guid Id { get; init; } = Guid.NewGuid();
    public Guid MemoryStoreId { get; init; }
    public string Key { get; set; } = string.Empty;
    public string Content { get; set; } = string.Empty;
    public DateTime CreatedAt { get; init; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
}
