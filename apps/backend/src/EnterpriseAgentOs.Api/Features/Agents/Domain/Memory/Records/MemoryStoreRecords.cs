namespace EnterpriseAgentOs.Domain.Features.Data;

public sealed class MemoryStoreRecord
{
    public Guid Id { get; init; } = Guid.NewGuid();
    public Guid OwnerId { get; init; }
    public string DisplayName { get; set; } = string.Empty;
    public DateTime CreatedAt { get; init; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    public static MemoryStoreRecord Create(Guid ownerId, string displayName) => new()
    {
        OwnerId = ownerId,
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
