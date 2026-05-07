namespace EnterpriseAgentOs.Infrastructure.Common.Entities;

public sealed class MemoryStoreEntryEntity
{
    public Guid Id { get; set; }
    public Guid MemoryStoreId { get; set; }
    public string Key { get; set; } = string.Empty;
    public string Content { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
    public MemoryStoreEntity? MemoryStore { get; set; }
}
