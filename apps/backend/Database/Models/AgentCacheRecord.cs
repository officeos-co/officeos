namespace EnterpriseAgentOs.Api.Database.Models;

public sealed class AgentCacheRecord
{
    public Guid Id { get; init; } = Guid.NewGuid();
    public Guid AgentId { get; set; }

    [Required, MaxLength(128)]
    public string CacheKey { get; set; } = string.Empty;

    [Required, MaxLength(128)]
    public string Model { get; set; } = string.Empty;

    [Required]
    public string Response { get; set; } = string.Empty;

    public int TokenCount { get; set; }

    public DateTime CreatedAt { get; init; } = DateTime.UtcNow;
    public DateTime? ExpiresAt { get; set; }
    public DateTime AccessedAt { get; set; } = DateTime.UtcNow;
}
