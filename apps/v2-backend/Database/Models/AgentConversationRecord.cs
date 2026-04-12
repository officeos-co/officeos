
namespace EnterpriseAgentOs.Api.Database.Models;

public sealed class AgentConversationRecord
{
    public Guid Id { get; init; } = Guid.NewGuid();
    public Guid AgentId { get; set; }
    public string Role { get; set; } = string.Empty;
    public string Content { get; set; } = string.Empty;
    public string? SessionId { get; set; }
    public DateTime CreatedAt { get; init; } = DateTime.UtcNow;
}
