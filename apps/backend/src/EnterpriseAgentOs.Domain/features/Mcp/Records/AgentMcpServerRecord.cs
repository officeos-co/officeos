namespace EnterpriseAgentOs.Domain.Features.Mcp;

public sealed class AgentMcpServerRecord
{
    public Guid Id { get; init; } = Guid.NewGuid();
    public Guid AgentId { get; init; }
    public string McpServerName { get; init; } = string.Empty;
    public DateTime CreatedAt { get; init; } = DateTime.UtcNow;
}
