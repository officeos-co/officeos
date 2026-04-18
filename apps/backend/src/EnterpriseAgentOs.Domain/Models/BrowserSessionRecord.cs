namespace EnterpriseAgentOs.Domain.Models;

public sealed class BrowserSessionRecord
{
    public Guid Id { get; init; } = Guid.NewGuid();

    public Guid AgentId { get; set; }

    [Required]
    public string RuntimeSessionId { get; set; } = string.Empty;

    public string? CookiesJson { get; set; }

    public DateTime CreatedAt { get; init; } = DateTime.UtcNow;

    public DateTime LastAccessedAt { get; set; } = DateTime.UtcNow;
}
