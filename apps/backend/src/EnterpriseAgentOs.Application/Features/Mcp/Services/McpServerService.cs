namespace EnterpriseAgentOs.Application.Features.Mcp;

internal sealed class McpServerService : IMcpServerService
{
    private readonly IAgentMcpServerRepository _agentServerRepository;
    private readonly IMcpCredentialRepository _credentialRepository;
    private readonly CredentialProtector _credentialProtector;

    public McpServerService(
        IAgentMcpServerRepository agentServers,
        IMcpCredentialRepository credentials,
        CredentialProtector protector)
    {
        _agentServerRepository = agentServers;
        _credentialRepository = credentials;
        _credentialProtector = protector;
    }

    public Task<IReadOnlyList<McpServerRecord>> ListAsync(CancellationToken ct)
        => Task.FromResult<IReadOnlyList<McpServerRecord>>(OrderedBuiltins());

    public Task<McpServerRecord?> GetAsync(string name, CancellationToken ct)
        => Task.FromResult(McpServerRegistry.GetBuiltin(name));

    public Task<McpServerRecord> RegisterAsync(McpServerRecord server, CancellationToken ct)
        => throw new NotSupportedException("MCP server catalog definitions are registry-only.");

    public Task DeleteAsync(string name, CancellationToken ct)
        => throw new NotSupportedException("MCP server catalog definitions are registry-only.");

    public async Task<IReadOnlyList<McpServerRecord>> ListForAgentAsync(Guid agentId, CancellationToken ct)
    {
        var names = await _agentServerRepository.ListServerNamesForAgentAsync(agentId, ct);
        if (names.Count == 0) return [];

        var allowed = names.ToHashSet(StringComparer.OrdinalIgnoreCase);
        return OrderedBuiltins().Where(s => allowed.Contains(s.Name)).ToList();
    }

    public async Task AssignToAgentAsync(Guid agentId, string serverName, CancellationToken ct)
    {
        var server = await GetAsync(serverName, ct)
            ?? throw new InvalidOperationException($"MCP server '{serverName}' was not found.");

        await _agentServerRepository.AssignAsync(agentId, server.Name, ct);
    }

    public Task UnassignFromAgentAsync(Guid agentId, string serverName, CancellationToken ct)
        => _agentServerRepository.UnassignAsync(agentId, serverName, ct);

    public async Task SaveCredentialAsync(string serverName, Dictionary<string, string> fields, CancellationToken ct)
    {
        var encrypted = _credentialProtector.Protect(fields);
        await _credentialRepository.UpsertAsync(new McpCredentialRecord
        {
            McpServerName = serverName,
            EncryptedCredentials = encrypted,
            ConfiguredAt = DateTime.UtcNow,
        }, ct);
    }

    public async Task<Dictionary<string, string>> GetDecryptedCredentialAsync(string serverName, CancellationToken ct)
    {
        var record = await _credentialRepository.GetByServerNameAsync(serverName, ct);
        if (record is null) return new();
        return _credentialProtector.Unprotect(record.EncryptedCredentials);
    }

    private static IReadOnlyList<McpServerRecord> OrderedBuiltins()
        => McpServerRegistry.BuiltinServers
            .OrderBy(s => s.Category)
            .ThenBy(s => s.Title)
            .ToList();
}
