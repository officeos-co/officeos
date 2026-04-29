using EnterpriseAgentOs.Domain.Features.Analytics;

namespace EnterpriseAgentOs.Infrastructure.Common.Entities;

public sealed class AgentLogEntity
{
    public Guid Id { get; set; }
    public Guid? AgentId { get; set; }
    public DateTime Time { get; set; }
    public AgentLogType Type { get; set; }
    public string? Tool { get; set; }
    public string? Integration { get; set; }
    public string? Channel { get; set; }
    public string Content { get; set; } = string.Empty;
    public int? DurationMs { get; set; }
    public int? InputTokens { get; set; }
    public int? OutputTokens { get; set; }
    public string? CorrelationId { get; set; }
    public AgentEntity? Agent { get; set; }
}
