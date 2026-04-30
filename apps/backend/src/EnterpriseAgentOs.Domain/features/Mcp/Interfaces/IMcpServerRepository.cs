namespace EnterpriseAgentOs.Domain.Features.Mcp;

public interface IMcpServerRepository
{
    Task<IReadOnlyList<McpServerRecord>> ListAsync(CancellationToken ct = default);
    Task<McpServerRecord?> GetByNameAsync(string name, CancellationToken ct = default);
    Task<McpServerRecord> UpsertAsync(McpServerRecord server, CancellationToken ct = default);
    Task DeleteAsync(string name, CancellationToken ct = default);
}
