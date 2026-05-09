namespace EnterpriseAgentOs.Database.Models;

public sealed class BrowserSessionEntity
{
    public Guid Id { get; set; }
    public Guid AgentId { get; set; }
    public string RuntimeSessionId { get; set; } = string.Empty;
    public string? CookiesJson { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime LastAccessedAt { get; set; }
}
