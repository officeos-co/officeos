namespace EnterpriseAgentOs.Domain.Features.Mcp;

public interface IAgentMcpServerRepository
{
    Task<IReadOnlyList<string>> ListServerNamesForAgentAsync(Guid agentId, CancellationToken ct = default);
    Task AssignAsync(Guid agentId, string mcpServerName, CancellationToken ct = default);
    Task UnassignAsync(Guid agentId, string mcpServerName, CancellationToken ct = default);
    Task UnassignServerFromAllAgentsAsync(string mcpServerName, CancellationToken ct = default);
}
