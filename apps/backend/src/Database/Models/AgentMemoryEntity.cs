namespace OffceOs.Database.Models;

public sealed class AgentMemoryEntity
{
    public Guid Id { get; set; }
    public Guid AgentId { get; set; }
    public string Key { get; set; } = null!;
    public string Content { get; set; } = null!;
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
    public AgentEntity? Agent { get; set; }
}
