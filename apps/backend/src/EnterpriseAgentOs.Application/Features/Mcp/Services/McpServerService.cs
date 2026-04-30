namespace EnterpriseAgentOs.Application.Features.Mcp;

internal sealed class McpServerService : IMcpServerService
{
    private readonly IMcpServerRepository _serverRepository;
    private readonly IAgentMcpServerRepository _agentServerRepository;
    private readonly IMcpCredentialRepository _credentialRepository;
    private readonly CredentialProtector _credentialProtector;

    public McpServerService(
        IMcpServerRepository servers,
        IAgentMcpServerRepository agentServers,
        IMcpCredentialRepository credentials,
        CredentialProtector protector)
    {
        _serverRepository = servers;
        _agentServerRepository = agentServers;
        _credentialRepository = credentials;
        _credentialProtector = protector;
    }

    public Task<IReadOnlyList<McpServerRecord>> ListAsync(CancellationToken ct)
        => _serverRepository.ListAsync(ct);

    public Task<McpServerRecord?> GetAsync(string name, CancellationToken ct)
        => _serverRepository.GetByNameAsync(name, ct);

    public Task<McpServerRecord> RegisterAsync(McpServerRecord server, CancellationToken ct)
        => _serverRepository.UpsertAsync(server, ct);

    public Task DeleteAsync(string name, CancellationToken ct)
        => _serverRepository.DeleteAsync(name, ct);

    public Task<IReadOnlyList<McpServerRecord>> ListForAgentAsync(Guid agentId, CancellationToken ct)
        => _agentServerRepository.ListServersForAgentAsync(agentId, ct);

    public Task AssignToAgentAsync(Guid agentId, string serverName, CancellationToken ct)
        => _agentServerRepository.AssignAsync(agentId, serverName, ct);

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
}
