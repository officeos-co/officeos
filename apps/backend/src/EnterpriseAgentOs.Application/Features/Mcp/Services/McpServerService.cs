namespace EnterpriseAgentOs.Application.Features.Mcp;

internal sealed class McpServerService : IMcpServerService
{
    private readonly IAgentMcpServerRepository _agentServerRepository;
    private readonly IMcpCredentialRepository _credentialRepository;
    private readonly IOAuthTokenRepository _oauthTokenRepository;
    private readonly CredentialProtector _credentialProtector;
    private readonly GoogleOAuthConfig _googleOAuthConfig;
    private readonly ILogger<McpServerService> _logger;

    public McpServerService(
        IAgentMcpServerRepository agentServers,
        IMcpCredentialRepository credentials,
        IOAuthTokenRepository oauthTokens,
        CredentialProtector protector,
        GoogleOAuthConfig googleOAuthConfig,
        ILogger<McpServerService> logger)
    {
        _agentServerRepository = agentServers;
        _credentialRepository = credentials;
        _oauthTokenRepository = oauthTokens;
        _credentialProtector = protector;
        _googleOAuthConfig = googleOAuthConfig;
        _logger = logger;
    }

    public async Task<IReadOnlyList<McpServerRecord>> ListAsync(CancellationToken ct)
        => await WithOAuthStatusAsync(OrderedBuiltins(), ct);

    public async Task<McpServerRecord?> GetAsync(string name, CancellationToken ct)
    {
        var server = McpServerRegistry.GetBuiltin(name);
        if (server is null) return null;

        return (await WithOAuthStatusAsync([server], ct)).FirstOrDefault();
    }

    public Task<McpServerRecord> RegisterAsync(McpServerRecord server, CancellationToken ct)
        => throw new NotSupportedException("MCP server catalog definitions are registry-only.");

    public Task DeleteAsync(string name, CancellationToken ct)
        => throw new NotSupportedException("MCP server catalog definitions are registry-only.");

    public async Task<IReadOnlyList<McpServerRecord>> ListForAgentAsync(Guid agentId, CancellationToken ct)
    {
        var names = await _agentServerRepository.ListServerNamesForAgentAsync(agentId, ct);
        _logger.LogDebug("MCP catalog for agent {AgentId}: assigned servers [{Servers}]", agentId, string.Join(", ", names));
        if (names.Count == 0) return [];

        var allowed = names.ToHashSet(StringComparer.OrdinalIgnoreCase);
        return await WithOAuthStatusAsync(
            OrderedBuiltins().Where(s => allowed.Contains(s.Name)).ToList(),
            ct);
    }

    public async Task AssignToAgentAsync(Guid agentId, string serverName, CancellationToken ct)
    {
        var server = await GetAsync(serverName, ct)
            ?? throw new InvalidOperationException($"MCP server '{serverName}' was not found.");

        await _agentServerRepository.AssignAsync(agentId, server.Name, ct);
        _logger.LogInformation("Assigned MCP server {Server} to agent {AgentId}", server.Name, agentId);
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
        var server = McpServerRegistry.GetBuiltin(serverName);
        if (!string.IsNullOrWhiteSpace(server?.OauthProvider))
            return await GetOAuthCredentialAsync(server, ct);

        var record = await _credentialRepository.GetByServerNameAsync(serverName, ct);
        if (record is null) return new();
        return _credentialProtector.Unprotect(record.EncryptedCredentials);
    }

    private async Task<Dictionary<string, string>> GetOAuthCredentialAsync(McpServerRecord server, CancellationToken ct)
    {
        var token = await _oauthTokenRepository.GetByProviderAsync(server.OauthProvider!, ct);
        if (token is null) return new();

        return server.OauthProvider switch
        {
            "github" => BuildGitHubEnvironment(token),
            "google" => BuildGoogleEnvironment(token),
            _ => new Dictionary<string, string>(),
        };
    }

    private Dictionary<string, string> BuildGitHubEnvironment(OAuthTokenRecord token)
    {
        var accessToken = UnprotectToken(token.EncryptedAccessToken);
        return string.IsNullOrWhiteSpace(accessToken)
            ? new Dictionary<string, string>()
            : new Dictionary<string, string> { ["GITHUB_PERSONAL_ACCESS_TOKEN"] = accessToken };
    }

    private Dictionary<string, string> BuildGoogleEnvironment(OAuthTokenRecord token)
    {
        var refreshToken = UnprotectToken(token.EncryptedRefreshToken);
        if (string.IsNullOrWhiteSpace(refreshToken)
            || string.IsNullOrWhiteSpace(_googleOAuthConfig.ClientId)
            || string.IsNullOrWhiteSpace(_googleOAuthConfig.ClientSecret))
            return new Dictionary<string, string>();

        return new Dictionary<string, string>
        {
            ["GOOGLE_CLIENT_ID"] = _googleOAuthConfig.ClientId,
            ["GOOGLE_CLIENT_SECRET"] = _googleOAuthConfig.ClientSecret,
            ["GOOGLE_REFRESH_TOKEN"] = refreshToken,
            ["GOOGLE_TOKEN_SCOPE"] = string.Join(' ', token.GetScopeSet()),
        };
    }

    private string? UnprotectToken(string? encrypted)
    {
        if (string.IsNullOrWhiteSpace(encrypted)) return null;
        return _credentialProtector.Unprotect(encrypted).GetValueOrDefault("token");
    }

    private async Task<IReadOnlyList<McpServerRecord>> WithOAuthStatusAsync(
        IReadOnlyList<McpServerRecord> servers,
        CancellationToken ct)
    {
        var providers = servers
            .Select(s => s.OauthProvider)
            .Where(p => !string.IsNullOrWhiteSpace(p))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();

        if (providers.Count == 0) return servers;

        var configured = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (var provider in providers)
        {
            var token = await _oauthTokenRepository.GetByProviderAsync(provider!, ct);
            if (!string.IsNullOrWhiteSpace(token?.EncryptedAccessToken)
                || !string.IsNullOrWhiteSpace(token?.EncryptedRefreshToken))
            {
                configured.Add(provider!);
            }
            else
            {
                _logger.LogDebug("MCP OAuth status for provider {Provider}: no stored token", provider);
            }
        }

        foreach (var server in servers.Where(s => !string.IsNullOrWhiteSpace(s.OauthProvider)))
        {
            var token = await _oauthTokenRepository.GetByProviderAsync(server.OauthProvider!, ct);
            if (token is null)
                continue;

            var scopes = ParseScopes(server.OauthScopesJson);
            var missingScopes = token.MissingScopes(scopes);
            if (missingScopes.Count > 0)
            {
                _logger.LogDebug(
                    "MCP OAuth status for {Server}: provider {Provider} missing scopes [{MissingScopes}]",
                    server.Name,
                    server.OauthProvider,
                    string.Join(", ", missingScopes));
            }
        }

        return servers.Select(s => string.IsNullOrWhiteSpace(s.OauthProvider)
            ? s
            : CopyWithOauthConfigured(s, configured.Contains(s.OauthProvider))).ToList();
    }

    private static IReadOnlyList<string> ParseScopes(string? scopesJson)
    {
        if (string.IsNullOrWhiteSpace(scopesJson))
            return [];

        try
        {
            var parsed = JsonSerializer.Deserialize<JsonElement>(scopesJson);
            return parsed.ValueKind == JsonValueKind.Array
                ? parsed.EnumerateArray().Select(s => s.GetString()).Where(s => !string.IsNullOrWhiteSpace(s)).Select(s => s!).ToList()
                : [];
        }
        catch
        {
            return [];
        }
    }

    private static McpServerRecord CopyWithOauthConfigured(McpServerRecord server, bool configured) => new()
    {
        Id = server.Id,
        Name = server.Name,
        Title = server.Title,
        Description = server.Description,
        TransportType = server.TransportType,
        Command = server.Command,
        Args = server.Args,
        Url = server.Url,
        Logo = server.Logo,
        Category = server.Category,
        CredentialFieldsJson = server.CredentialFieldsJson,
        OauthProvider = server.OauthProvider,
        OauthScopesJson = server.OauthScopesJson,
        OauthConfigured = configured,
        Subtitle = server.Subtitle,
        AuthorName = server.AuthorName,
        AuthorUrl = server.AuthorUrl,
        DocumentationUrl = server.DocumentationUrl,
        RepositoryUrl = server.RepositoryUrl,
        ToolsJson = server.ToolsJson,
        IsBuiltin = server.IsBuiltin,
        CreatedAt = server.CreatedAt,
    };

    private static IReadOnlyList<McpServerRecord> OrderedBuiltins()
        => McpServerRegistry.BuiltinServers
            .OrderBy(s => s.Category)
            .ThenBy(s => s.Title)
            .ToList();
}
