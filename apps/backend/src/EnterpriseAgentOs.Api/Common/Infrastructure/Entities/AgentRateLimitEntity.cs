namespace EnterpriseAgentOs.Infrastructure.Common.Entities;

public sealed class AgentRateLimitEntity
{
    public Guid Id { get; set; }
    public Guid AgentId { get; set; }
    public string BucketKey { get; set; } = string.Empty;
    public DateTime WindowStart { get; set; }
    public int Count { get; set; }
}
