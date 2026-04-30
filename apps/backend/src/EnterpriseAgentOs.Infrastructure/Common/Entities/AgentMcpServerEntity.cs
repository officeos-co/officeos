namespace EnterpriseAgentOs.Infrastructure.Common.Entities;

public sealed class AgentMcpServerEntity
{
    public Guid Id { get; set; }
    public Guid AgentId { get; set; }
    public string McpServerName { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
