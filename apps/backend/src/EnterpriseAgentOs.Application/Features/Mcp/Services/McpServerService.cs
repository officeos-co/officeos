namespace EnterpriseAgentOs.Application.Features.Agents.Integrations;

internal sealed class IntegrationDefinitionService : IIntegrationDefinitionService
{
    private readonly IAgentIntegrationDefinitionRepository _agentServerRepository;
    private readonly IIntegrationDefinitionRepository _serverRepository;
    private readonly IIntegrationCredentialRepository _credentialRepository;
    private readonly IOAuthTokenRepository _oauthTokenRepository;
    private readonly CredentialProtector _credentialProtector;
    private readonly GoogleOAuthConfig _googleOAuthConfig;
    private readonly ILogger<IntegrationDefinitionService> _logger;

    public IntegrationDefinitionService(
        IAgentIntegrationDefinitionRepository agentServers,
        IIntegrationDefinitionRepository servers,
        IIntegrationCredentialRepository credentials,
        IOAuthTokenRepository oauthTokens,
        CredentialProtector protector,
        GoogleOAuthConfig googleOAuthConfig,
        ILogger<IntegrationDefinitionService> logger)
    {
        _agentServerRepository = agentServers;
        _serverRepository = servers;
        _credentialRepository = credentials;
        _oauthTokenRepository = oauthTokens;
        _credentialProtector = protector;
        _googleOAuthConfig = googleOAuthConfig;
        _logger = logger;
    }

    public async Task<IReadOnlyList<IntegrationDefinitionRecord>> ListAsync(CancellationToken ct = default)
        => await WithConnectionStatusAsync(await OrderedCatalogAsync(ct), ct);

    public async Task<IntegrationDefinitionRecord?> GetAsync(string name, CancellationToken ct = default)
    {
        var server = McpServerRegistry.GetBuiltin(name)
            ?? await _serverRepository.GetByNameAsync(name, ct);
        if (server is null) return null;

        return (await WithConnectionStatusAsync([server], ct)).FirstOrDefault();
    }

    public async Task<IntegrationDefinitionRecord> RegisterAsync(IntegrationDefinitionRecord server, CancellationToken ct = default)
    {
        if (McpServerRegistry.GetBuiltin(server.Name) is not null)
            throw new InvalidOperationException($"MCP server '{server.Name}' is built in and cannot be overwritten.");

        var saved = await _serverRepository.UpsertAsync(CopyAsCustom(server), ct);
        return (await WithConnectionStatusAsync([saved], ct)).First();
    }

    public async Task DeleteAsync(string name, CancellationToken ct = default)
    {
        if (McpServerRegistry.GetBuiltin(name) is not null)
            throw new InvalidOperationException($"MCP server '{name}' is built in and cannot be deleted.");

        await _serverRepository.DeleteAsync(name, ct);
        await _credentialRepository.DeleteAsync(name, ct);
        await _agentServerRepository.UnassignServerFromAllAgentsAsync(name, ct);
    }

    public async Task<IReadOnlyList<IntegrationDefinitionRecord>> ListForAgentAsync(Guid agentId, CancellationToken ct = default)
    {
        var names = await _agentServerRepository.ListIntegrationNamesForAgentAsync(agentId, ct);
        _logger.LogDebug("MCP catalog for agent {AgentId}: assigned servers [{Servers}]", agentId, string.Join(", ", names));
        if (names.Count == 0) return [];

        var allowed = names.ToHashSet(StringComparer.OrdinalIgnoreCase);
        return await WithConnectionStatusAsync(
            (await OrderedCatalogAsync(ct)).Where(s => allowed.Contains(s.Name)).ToList(),
            ct);
    }

    public async Task AssignToAgentAsync(Guid agentId, string integrationName, CancellationToken ct = default)
    {
        var server = await GetAsync(integrationName, ct)
            ?? throw new InvalidOperationException($"MCP server '{integrationName}' was not found.");

        await _agentServerRepository.AssignAsync(agentId, server.Name, ct);
        _logger.LogInformation("Assigned MCP server {Server} to agent {AgentId}", server.Name, agentId);
    }

    public Task UnassignFromAgentAsync(Guid agentId, string integrationName, CancellationToken ct = default)
        => _agentServerRepository.UnassignAsync(agentId, integrationName, ct);

    public async Task SaveCredentialAsync(string integrationName, Dictionary<string, string> fields, CancellationToken ct = default)
    {
        var encrypted = _credentialProtector.Protect(fields);
        await _credentialRepository.UpsertAsync(new IntegrationCredentialRecord
        {
            IntegrationName = integrationName,
            EncryptedCredentials = encrypted,
            ConfiguredAt = DateTime.UtcNow,
        }, ct);
    }

    public async Task<Dictionary<string, string>> GetDecryptedCredentialAsync(string integrationName, CancellationToken ct = default)
    {
        var server = McpServerRegistry.GetBuiltin(integrationName);
        if (!string.IsNullOrWhiteSpace(server?.OauthProvider))
            return await GetOAuthCredentialAsync(server, ct);

        var record = await _credentialRepository.GetByAsync(new IntegrationCredentialFilter { IntegrationName = integrationName }, ct);
        if (record is null) return new();
        return _credentialProtector.Unprotect(record.EncryptedCredentials);
    }

    private async Task<Dictionary<string, string>> GetOAuthCredentialAsync(IntegrationDefinitionRecord server, CancellationToken ct)
    {
        var token = await _oauthTokenRepository.GetByAsync(new OAuthTokenFilter { Provider = server.OauthProvider! }, ct);
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

    private async Task<IReadOnlyList<IntegrationDefinitionRecord>> WithConnectionStatusAsync(
        IReadOnlyList<IntegrationDefinitionRecord> servers,
        CancellationToken ct)
    {
        var providers = servers
            .Select(s => s.OauthProvider)
            .Where(p => !string.IsNullOrWhiteSpace(p))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();

        var configured = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (var provider in providers)
        {
            var token = await _oauthTokenRepository.GetByAsync(new OAuthTokenFilter { Provider = provider! }, ct);
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
            var token = await _oauthTokenRepository.GetByAsync(new OAuthTokenFilter { Provider = server.OauthProvider! }, ct);
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

        var withOAuth = servers.Select(s => string.IsNullOrWhiteSpace(s.OauthProvider)
            ? s
            : CopyWithOauthConfigured(s, configured.Contains(s.OauthProvider))).ToList();

        var credentialConfigured = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (var server in withOAuth.Where(s => string.IsNullOrWhiteSpace(s.OauthProvider)))
        {
            var record = await _credentialRepository.GetByAsync(new IntegrationCredentialFilter { IntegrationName = server.Name }, ct);
            if (!string.IsNullOrWhiteSpace(record?.EncryptedCredentials))
                credentialConfigured.Add(server.Name);
        }

        return withOAuth.Select(s => CopyWithCredentialConfigured(s, credentialConfigured.Contains(s.Name))).ToList();
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

    private static IntegrationDefinitionRecord CopyWithOauthConfigured(IntegrationDefinitionRecord server, bool configured) => new()
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
        CredentialConfigured = server.CredentialConfigured,
        CreatedAt = server.CreatedAt,
    };

    private static IntegrationDefinitionRecord CopyWithCredentialConfigured(IntegrationDefinitionRecord server, bool configured) => new()
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
        OauthConfigured = server.OauthConfigured,
        Subtitle = server.Subtitle,
        AuthorName = server.AuthorName,
        AuthorUrl = server.AuthorUrl,
        DocumentationUrl = server.DocumentationUrl,
        RepositoryUrl = server.RepositoryUrl,
        ToolsJson = server.ToolsJson,
        IsBuiltin = server.IsBuiltin,
        CredentialConfigured = configured,
        CreatedAt = server.CreatedAt,
    };

    private static IntegrationDefinitionRecord CopyAsCustom(IntegrationDefinitionRecord server) => new()
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
        Subtitle = server.Subtitle,
        AuthorName = server.AuthorName,
        AuthorUrl = server.AuthorUrl,
        DocumentationUrl = server.DocumentationUrl,
        RepositoryUrl = server.RepositoryUrl,
        ToolsJson = server.ToolsJson,
        IsBuiltin = false,
        CreatedAt = server.CreatedAt == default ? DateTime.UtcNow : server.CreatedAt,
    };

    private async Task<IReadOnlyList<IntegrationDefinitionRecord>> OrderedCatalogAsync(CancellationToken ct)
    {
        var custom = await _serverRepository.ListAsync(ct);
        return OrderedBuiltins()
            .Concat(custom)
            .OrderBy(s => s.Category)
            .ThenBy(s => s.Title)
            .ToList();
    }

    private static IReadOnlyList<IntegrationDefinitionRecord> OrderedBuiltins()
        => McpServerRegistry.BuiltinServers
            .OrderBy(s => s.Category)
            .ThenBy(s => s.Title)
            .ToList();
}
