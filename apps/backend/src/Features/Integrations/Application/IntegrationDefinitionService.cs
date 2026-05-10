namespace OffceOs.Application.Features.Integrations;

internal sealed class IntegrationDefinitionService : IIntegrationDefinitionService
{
    private readonly IAgentIntegrationRepository _agentIntegrationRepository;
    private readonly IAgentRepository _agentRepository;
    private readonly IIntegrationDefinitionRepository _integrationDefinitionRepository;
    private readonly IIntegrationCredentialRepository _integrationCredentialRepository;
    private readonly IOAuthTokenRepository _oauthTokenRepository;
    private readonly IIntegrationDeploymentRepository _integrationDeploymentRepository;
    private readonly IWorkspaceRepository _workspaceRepository;
    private readonly IWorkspaceMemberRepository _workspaceMemberRepository;
    private readonly IOrganizationRepository _organizationRepository;
    private readonly CredentialProtector _credentialProtector;
    private readonly GoogleOAuthConfig _googleOAuthConfig;
    private readonly ILogger<IntegrationDefinitionService> _logger;

    public IntegrationDefinitionService(
        IAgentIntegrationRepository agentIntegrations,
        IAgentRepository agentRepository,
        IIntegrationDefinitionRepository definitions,
        IIntegrationCredentialRepository credentials,
        IOAuthTokenRepository oauthTokens,
        CredentialProtector protector,
        GoogleOAuthConfig googleOAuthConfig,
        ILogger<IntegrationDefinitionService> logger,
        IIntegrationDeploymentRepository integrationDeploymentRepository,
        IWorkspaceRepository workspaceRepository,
        IWorkspaceMemberRepository workspaceMemberRepository,
        IOrganizationRepository organizationRepository)
    {
        _agentIntegrationRepository = agentIntegrations;
        _agentRepository = agentRepository;
        _integrationDefinitionRepository = definitions;
        _integrationCredentialRepository = credentials;
        _oauthTokenRepository = oauthTokens;
        _integrationDeploymentRepository = integrationDeploymentRepository;
        _workspaceRepository = workspaceRepository;
        _workspaceMemberRepository = workspaceMemberRepository;
        _organizationRepository = organizationRepository;
        _credentialProtector = protector;
        _googleOAuthConfig = googleOAuthConfig;
        _logger = logger;
    }

    public async Task<IReadOnlyList<IntegrationDefinitionRecord>> ListAsync(Guid ownerId, Guid? workspaceId = null, CancellationToken ct = default)
        => await WithConnectionStatusAsync(ownerId, workspaceId, await OrderedCatalogAsync(ownerId, workspaceId, ct), ct);

    public async Task<IntegrationDefinitionRecord?> GetAsync(Guid ownerId, string name, Guid? workspaceId = null, CancellationToken ct = default)
    {
        var server = IntegrationDefinitionProvider.GetBuiltin(name)
            ?? await _integrationDefinitionRepository.GetByNameAsync(ownerId, name, workspaceId, ct);
        if (server is null) return null;
        if (!await IsAvailableInWorkspaceAsync(server.Name, workspaceId, ct))
            return null;

        return (await WithConnectionStatusAsync(ownerId, workspaceId, [server], ct)).FirstOrDefault();
    }

    public async Task<IntegrationDefinitionRecord> RegisterAsync(Guid ownerId, Guid workspaceId, IntegrationDefinitionRecord server, CancellationToken ct = default)
    {
        await RequireWorkspaceEditorAsync(ownerId, workspaceId, ct);

        if (IntegrationDefinitionProvider.GetBuiltin(server.Name) is not null)
            throw new InvalidOperationException($"integration '{server.Name}' is built in and cannot be overwritten.");

        var saved = await _integrationDefinitionRepository.UpsertAsync(ownerId, workspaceId, CopyAsCustom(ownerId, workspaceId, server), ct);
        await EnsureDeploymentForRegisteredWorkspaceAsync(ownerId, workspaceId, saved.Name, ct);
        return (await WithConnectionStatusAsync(ownerId, workspaceId, [saved], ct)).First();
    }

    public async Task DeleteAsync(Guid ownerId, string name, Guid? workspaceId = null, CancellationToken ct = default)
    {
        if (workspaceId.HasValue)
            await RequireWorkspaceEditorAsync(ownerId, workspaceId.Value, ct);

        if (IntegrationDefinitionProvider.GetBuiltin(name) is not null)
            throw new InvalidOperationException($"integration '{name}' is built in and cannot be deleted.");

        await _integrationDefinitionRepository.DeleteAsync(ownerId, name, workspaceId, ct);
        await _integrationCredentialRepository.DeleteAsync(ownerId, name, workspaceId, ct);
        await _agentIntegrationRepository.UnassignIntegrationFromOwnerAgentsAsync(ownerId, name, ct);
    }

    public async Task<IReadOnlyList<IntegrationDefinitionRecord>> ListForAgentAsync(Guid agentId, Guid? ownerId = null, CancellationToken ct = default)
    {
        var agent = await _agentRepository.GetByAsync(new AgentFilter { Id = agentId, OwnerId = ownerId }, ct);
        if (agent is null) return [];
        var effectiveOwnerId = agent.OwnerId ?? ownerId;
        if (!effectiveOwnerId.HasValue) return [];

        var names = await _agentIntegrationRepository.ListIntegrationNamesForAgentAsync(agentId, ct);
        _logger.LogDebug("integration catalog for agent {AgentId}: assigned integrations [{Integrations}]", agentId, string.Join(", ", names));
        if (names.Count == 0) return [];

        var allowed = names.ToHashSet(StringComparer.OrdinalIgnoreCase);
        return await WithConnectionStatusAsync(
            effectiveOwnerId.Value,
            agent.WorkspaceId,
            (await OrderedCatalogAsync(effectiveOwnerId.Value, agent.WorkspaceId, ct)).Where(s => allowed.Contains(s.Name)).ToList(),
            ct);
    }

    public async Task AssignToAgentAsync(Guid agentId, string integrationName, Guid? ownerId = null, CancellationToken ct = default)
    {
        var agent = await _agentRepository.GetByAsync(new AgentFilter { Id = agentId, OwnerId = ownerId }, ct)
            ?? throw new InvalidOperationException($"agent '{agentId}' was not found.");
        var effectiveOwnerId = agent.OwnerId ?? ownerId
            ?? throw new InvalidOperationException($"agent '{agentId}' has no owner.");
        var server = await GetAsync(effectiveOwnerId, integrationName, agent.WorkspaceId, ct)
            ?? throw new InvalidOperationException($"integration '{integrationName}' was not found.");
        if (!await IsAvailableInWorkspaceAsync(server.Name, agent.WorkspaceId, ct))
            throw new InvalidOperationException($"integration '{integrationName}' is not deployed to this workspace.");

        await _agentIntegrationRepository.AssignAsync(agentId, server.Name, ct);
        _logger.LogInformation("Assigned integration {Integration} to agent {AgentId}", server.Name, agentId);
    }

    public Task UnassignFromAgentAsync(Guid agentId, string integrationName, CancellationToken ct = default)
        => _agentIntegrationRepository.UnassignAsync(agentId, integrationName, ct);

    public async Task SaveCredentialAsync(Guid ownerId, Guid workspaceId, string integrationName, Dictionary<string, string> fields, CancellationToken ct = default)
    {
        await RequireWorkspaceEditorAsync(ownerId, workspaceId, ct);

        var encrypted = _credentialProtector.Protect(fields);
        await _integrationCredentialRepository.UpsertAsync(new IntegrationCredentialRecord
        {
            OwnerId = ownerId,
            WorkspaceId = workspaceId,
            IntegrationName = integrationName,
            EncryptedCredentials = encrypted,
            ConfiguredAt = DateTime.UtcNow,
        }, ct);
    }

    public async Task<Dictionary<string, string>> GetDecryptedCredentialAsync(string integrationName, Guid? ownerId = null, Guid? workspaceId = null, CancellationToken ct = default)
    {
        if (!ownerId.HasValue) return new();

        var server = IntegrationDefinitionProvider.GetBuiltin(integrationName);
        if (!string.IsNullOrWhiteSpace(server?.OauthProvider))
            return await GetOAuthCredentialAsync(ownerId.Value, server, ct);

        var record = await _integrationCredentialRepository.GetByAsync(new IntegrationCredentialFilter
        {
            OwnerId = ownerId.Value,
            WorkspaceId = workspaceId,
            IntegrationName = integrationName,
        }, ct);
        if (record is null) return new();
        return _credentialProtector.Unprotect(record.EncryptedCredentials);
    }

    private async Task<Dictionary<string, string>> GetOAuthCredentialAsync(Guid ownerId, IntegrationDefinitionRecord server, CancellationToken ct)
    {
        var token = await _oauthTokenRepository.GetByAsync(new OAuthTokenFilter { UserId = ownerId, Provider = server.OauthProvider! }, ct);
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
        Guid ownerId,
        Guid? workspaceId,
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
            var token = await _oauthTokenRepository.GetByAsync(new OAuthTokenFilter { UserId = ownerId, Provider = provider! }, ct);
            if (!string.IsNullOrWhiteSpace(token?.EncryptedAccessToken)
                || !string.IsNullOrWhiteSpace(token?.EncryptedRefreshToken))
            {
                configured.Add(provider!);
            }
            else
            {
                _logger.LogDebug("integration OAuth status for provider {Provider}: no stored token", provider);
            }
        }

        foreach (var server in servers.Where(s => !string.IsNullOrWhiteSpace(s.OauthProvider)))
        {
            var token = await _oauthTokenRepository.GetByAsync(new OAuthTokenFilter { UserId = ownerId, Provider = server.OauthProvider! }, ct);
            if (token is null)
                continue;

            var scopes = ParseScopes(server.OauthScopesJson);
            var missingScopes = token.MissingScopes(scopes);
            if (missingScopes.Count > 0)
            {
                _logger.LogDebug(
                    "integration OAuth status for {Integration}: provider {Provider} missing scopes [{MissingScopes}]",
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
            var record = await _integrationCredentialRepository.GetByAsync(new IntegrationCredentialFilter
            {
                OwnerId = ownerId,
                WorkspaceId = workspaceId,
                IntegrationName = server.Name,
            }, ct);
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
        OwnerId = server.OwnerId,
        WorkspaceId = server.WorkspaceId,
        Name = server.Name,
        Provider = server.Provider,
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
        CapabilitiesJson = server.CapabilitiesJson,
        Entities = server.Entities,
        IsBuiltin = server.IsBuiltin,
        CredentialConfigured = server.CredentialConfigured,
        CreatedAt = server.CreatedAt,
    };

    private static IntegrationDefinitionRecord CopyWithCredentialConfigured(IntegrationDefinitionRecord server, bool configured) => new()
    {
        Id = server.Id,
        OwnerId = server.OwnerId,
        WorkspaceId = server.WorkspaceId,
        Name = server.Name,
        Provider = server.Provider,
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
        CapabilitiesJson = server.CapabilitiesJson,
        Entities = server.Entities,
        IsBuiltin = server.IsBuiltin,
        CredentialConfigured = configured,
        CreatedAt = server.CreatedAt,
    };

    private static IntegrationDefinitionRecord CopyAsCustom(Guid ownerId, Guid workspaceId, IntegrationDefinitionRecord server) => new()
    {
        Id = server.Id,
        OwnerId = ownerId,
        WorkspaceId = workspaceId,
        Name = server.Name,
        Provider = server.Provider,
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
        CapabilitiesJson = server.CapabilitiesJson,
        Entities = server.Entities,
        IsBuiltin = false,
        CreatedAt = server.CreatedAt == default ? DateTime.UtcNow : server.CreatedAt,
    };

    private async Task<IReadOnlyList<IntegrationDefinitionRecord>> OrderedCatalogAsync(Guid ownerId, Guid? workspaceId, CancellationToken ct)
    {
        var custom = await _integrationDefinitionRepository.ListAsync(ownerId, workspaceId, ct);
        var ordered = OrderedBuiltins()
            .Concat(custom)
            .OrderBy(s => s.Category)
            .ThenBy(s => s.Title)
            .ToList();
        return await FilterCatalogForWorkspaceAsync(ordered, workspaceId, ct);
    }

    private async Task<IReadOnlyList<IntegrationDefinitionRecord>> FilterCatalogForWorkspaceAsync(
        IReadOnlyList<IntegrationDefinitionRecord> catalog,
        Guid? workspaceId,
        CancellationToken ct)
    {
        if (!workspaceId.HasValue)
            return catalog;

        var workspace = await _workspaceRepository.GetByAsync(new WorkspaceFilter { Id = workspaceId.Value }, ct);
        if (workspace?.OrganizationId is null)
            return catalog;

        var deployments = await _integrationDeploymentRepository.ListAsync(
            new IntegrationDeploymentFilter { WorkspaceId = workspaceId.Value, Enabled = true },
            ct);
        if (deployments.Count == 0)
            return catalog.Where(integration => integration.IsBuiltin).ToList();

        var deployed = deployments.Select(d => d.IntegrationName).ToHashSet(StringComparer.OrdinalIgnoreCase);
        return catalog.Where(integration => integration.IsBuiltin || deployed.Contains(integration.Name)).ToList();
    }

    private async Task<bool> IsAvailableInWorkspaceAsync(string integrationName, Guid? workspaceId, CancellationToken ct)
    {
        if (!workspaceId.HasValue)
            return true;

        var workspace = await _workspaceRepository.GetByAsync(new WorkspaceFilter { Id = workspaceId.Value }, ct);
        if (workspace?.OrganizationId is null)
            return true;

        if (IntegrationDefinitionProvider.GetBuiltin(integrationName) is not null)
            return true;

        var deployment = await _integrationDeploymentRepository.GetByAsync(
            new IntegrationDeploymentFilter
            {
                WorkspaceId = workspaceId.Value,
                IntegrationName = integrationName,
                Enabled = true,
            },
            ct);
        return deployment is not null;
    }

    private async Task EnsureDeploymentForRegisteredWorkspaceAsync(Guid ownerId, Guid workspaceId, string integrationName, CancellationToken ct)
    {
        var workspace = await _workspaceRepository.GetByAsync(new WorkspaceFilter { Id = workspaceId }, ct);
        if (workspace?.OrganizationId is null)
            return;

        await _integrationDeploymentRepository.UpsertAsync(new IntegrationDeploymentRecord
        {
            OrganizationId = workspace.OrganizationId.Value,
            WorkspaceId = workspaceId,
            IntegrationName = integrationName,
            CreatedById = ownerId,
            Enabled = true,
        }, ct);
    }

    private async Task RequireWorkspaceEditorAsync(Guid userId, Guid workspaceId, CancellationToken ct)
    {
        var workspace = await _workspaceRepository.GetByAsync(new WorkspaceFilter { Id = workspaceId }, ct)
            ?? throw new InvalidOperationException("Workspace not found.");
        if (workspace.OrganizationId is null)
            return;

        var membership = await _workspaceMemberRepository.GetByAsync(
            new WorkspaceMemberFilter { WorkspaceId = workspaceId, UserId = userId },
            ct);
        if (membership?.Role.CanEdit() == true)
            return;

        var members = await _organizationRepository.ListMembersAsync(workspace.OrganizationId.Value, ct);
        var member = members.FirstOrDefault(m => m.UserId == userId && m.Status == MemberStatus.Active);
        if (member?.Role is not (OrgRole.Owner or OrgRole.Admin))
            throw new InvalidOperationException("Only workspace editors or organization admins may manage integrations for organization workspaces.");
    }

    private static IReadOnlyList<IntegrationDefinitionRecord> OrderedBuiltins()
        => IntegrationDefinitionProvider.BuiltinDefinitions
            .OrderBy(s => s.Category)
            .ThenBy(s => s.Title)
            .ToList();
}
