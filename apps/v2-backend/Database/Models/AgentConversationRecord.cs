namespace EnterpriseAgentOs.Api.Database.Models;

public sealed class AgentConversationRecord
{
    public Guid Id { get; init; } = Guid.NewGuid();
    public Guid AgentId { get; set; }
    [Required, MaxLength(32)]
    public string Role { get; set; } = string.Empty;
    [Required]
    public string Content { get; set; } = string.Empty;
    [MaxLength(256)]
    public string? SessionId { get; set; }
    public DateTime CreatedAt { get; init; } = DateTime.UtcNow;
}
