namespace EnterpriseAgentOs.Domain.Features.Mcp;

public interface IAgentMcpServerRepository
{
    Task<IReadOnlyList<McpServerRecord>> ListServersForAgentAsync(Guid agentId, CancellationToken ct = default);
    Task AssignAsync(Guid agentId, string mcpServerName, CancellationToken ct = default);
    Task UnassignAsync(Guid agentId, string mcpServerName, CancellationToken ct = default);
}
