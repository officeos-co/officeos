namespace OffceOs.Database.Models;

public sealed class AgentLogEntity
{
    public Guid Id { get; set; }
    public Guid? AgentId { get; set; }
    public Guid? WorkspaceId { get; set; }
    public DateTime Time { get; set; }
    public AgentLogType Type { get; set; }
    public string? Tool { get; set; }
    public string? Integration { get; set; }
    public string? Channel { get; set; }
    public Guid? ChannelConnectionId { get; set; }
    public string Content { get; set; } = string.Empty;
    public int? DurationMs { get; set; }
    public int? InputTokens { get; set; }
    public int? OutputTokens { get; set; }
    public string? CorrelationId { get; set; }
    public Guid? RunId { get; set; }
    public Guid? ParentRunId { get; set; }
    public AgentEntity? Agent { get; set; }
    public WorkspaceEntity? Workspace { get; set; }
}
