namespace EnterpriseAgentOs.Domain.Features.Mcp;

public interface IMcpServerService
{
    Task<IReadOnlyList<McpServerRecord>> ListAsync(CancellationToken ct = default);
    Task<McpServerRecord?> GetAsync(string name, CancellationToken ct = default);
    Task<McpServerRecord> RegisterAsync(McpServerRecord server, CancellationToken ct = default);
    Task DeleteAsync(string name, CancellationToken ct = default);
    Task<IReadOnlyList<McpServerRecord>> ListForAgentAsync(Guid agentId, CancellationToken ct = default);
    Task AssignToAgentAsync(Guid agentId, string serverName, CancellationToken ct = default);
    Task UnassignFromAgentAsync(Guid agentId, string serverName, CancellationToken ct = default);
    Task SaveCredentialAsync(string serverName, Dictionary<string, string> fields, CancellationToken ct = default);
    Task<Dictionary<string, string>> GetDecryptedCredentialAsync(string serverName, CancellationToken ct = default);
}
