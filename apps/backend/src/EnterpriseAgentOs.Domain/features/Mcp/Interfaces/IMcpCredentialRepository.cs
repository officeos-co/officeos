namespace EnterpriseAgentOs.Domain.Features.Mcp;

public sealed record McpCredentialFilter
{
    public Guid? Id { get; init; }
    public string? ServerName { get; init; }
}

public interface IMcpCredentialRepository
{
    Task<McpCredentialRecord?> GetByAsync(McpCredentialFilter filter, CancellationToken ct = default);
    Task UpsertAsync(McpCredentialRecord credential, CancellationToken ct = default);
    Task DeleteAsync(string serverName, CancellationToken ct = default);
}
