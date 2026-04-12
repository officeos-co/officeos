
namespace EnterpriseAgentOs.Api.Database.Models;

public sealed class AgentCacheRecord
{
    public Guid Id { get; init; } = Guid.NewGuid();
    public Guid AgentId { get; set; }
    public string CacheKey { get; set; } = string.Empty;
    public string Model { get; set; } = string.Empty;
    public string Response { get; set; } = string.Empty;
    public DateTime CreatedAt { get; init; } = DateTime.UtcNow;
    public DateTime AccessedAt { get; set; } = DateTime.UtcNow;
}
