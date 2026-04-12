
namespace EnterpriseAgentOs.Api.Database.Models;

public sealed class AgentMemoryRecord
{
    public Guid Id { get; init; } = Guid.NewGuid();
    public Guid AgentId { get; set; }
    public string Key { get; set; } = string.Empty;
    public string Content { get; set; } = string.Empty;
    public string Category { get; set; } = string.Empty;
    public string Namespace { get; set; } = "default";
    public string? SessionId { get; set; }
    public string? SupersededBy { get; set; }
    public DateTime CreatedAt { get; init; } = DateTime.UtcNow;
    public DateTime? UpdatedAt { get; set; }
}
