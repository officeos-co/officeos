namespace EnterpriseAgentOs.Domain.Features.Mcp;

public interface IMcpCredentialRepository
{
    Task<McpCredentialRecord?> GetByServerNameAsync(string serverName, CancellationToken ct = default);
    Task UpsertAsync(McpCredentialRecord credential, CancellationToken ct = default);
    Task DeleteAsync(string serverName, CancellationToken ct = default);
}
