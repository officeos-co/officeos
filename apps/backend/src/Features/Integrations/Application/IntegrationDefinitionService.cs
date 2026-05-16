using OffceOs.Application.Features.ResourceLogs;
using OffceOs.Domain.Features.Agents;
using OffceOs.Domain.Features.Integrations;
using OffceOs.Domain.Features.Management;
using OffceOs.Domain.Common.ValueObjects;
namespace OffceOs.Application.Features.Integrations;

internal sealed class IntegrationDefinitionService : IIntegrationDefinitionService
{
    private readonly IAgentIntegrationRepository _agentIntegrationRepository;
    private readonly IAgentRepository _agentRepository;
    private readonly IIntegrationDefinitionRepository _integrationDefinitionRepository;
    private readonly IIntegrationCredentialRepository _integrationCredentialRepository;
    private readonly IIntegrationDeploymentRepository _integrationDeploymentRepository;
    private readonly IWorkspaceRepository _workspaceRepository;
    private readonly IWorkspaceMemberRepository _workspaceMemberRepository;
    private readonly IIntegrationCredentialEncryptionService _integrationCredentialEncryptionService;
    private readonly IResourceLogWriterService _resourceLogWriterService;

    public IntegrationDefinitionService(
        IAgentIntegrationRepository agentIntegrations,
        IAgentRepository agentRepository,
        IIntegrationDefinitionRepository definitions,
        IIntegrationCredentialRepository credentials,
        IIntegrationCredentialEncryptionService integrationCredentialEncryptionService,
        IResourceLogWriterService resourceLogWriterService,
        IIntegrationDeploymentRepository integrationDeploymentRepository,
        IWorkspaceRepository workspaceRepository,
        IWorkspaceMemberRepository workspaceMemberRepository)
    {
        _agentIntegrationRepository = agentIntegrations;
        _agentRepository = agentRepository;
        _integrationDefinitionRepository = definitions;
        _integrationCredentialRepository = credentials;
        _integrationDeploymentRepository = integrationDeploymentRepository;
        _workspaceRepository = workspaceRepository;
        _workspaceMemberRepository = workspaceMemberRepository;
        _integrationCredentialEncryptionService = integrationCredentialEncryptionService;
        _resourceLogWriterService = resourceLogWriterService;
    }

    public async Task<IReadOnlyList<IntegrationDefinitionRecord>> ListAsync(Guid ownerId, Guid? workspaceId = null, CancellationToken ct = default)
        => (await WithConnectionStatusAsync(ownerId, workspaceId, await OrderedCatalogAsync(ownerId, workspaceId, ct), ct))
            .Where(IsConnected)
            .ToList();

    public async Task<IReadOnlyList<IntegrationDefinitionRecord>> ListCatalogAsync(Guid ownerId, Guid? workspaceId = null, CancellationToken ct = default)
        => await WithConnectionStatusAsync(ownerId, workspaceId, await OrderedCatalogAsync(ownerId, workspaceId, ct), ct);

    public async Task<IntegrationDefinitionRecord?> GetAsync(Guid ownerId, string name, Guid? workspaceId = null, CancellationToken ct = default)
    {
        var server = IntegrationDefinitionProvider.GetBuiltin(name)
            ?? await _integrationDefinitionRepository.GetByNameAsync(ownerId, name, workspaceId, ct);
        if (server is null) return null;
        if (!await IsAvailableInWorkspaceAsync(server.Name, workspaceId, ct))
            return null;

        var configured = (await WithConnectionStatusAsync(ownerId, workspaceId, [server], ct)).FirstOrDefault();
        return configured is not null && IsConnected(configured) ? configured : null;
    }

    public async Task<IntegrationDefinitionRecord> RegisterAsync(Guid ownerId, Guid workspaceId, IntegrationDefinitionRecord server, CancellationToken ct = default)
    {
        await RequireWorkspaceEditorAsync(ownerId, workspaceId, ct);

        if (IntegrationDefinitionProvider.GetBuiltin(server.Name) is not null)
            throw new InvalidOperationException($"integration '{server.Name}' is built in and cannot be overwritten.");
        if (!RequiresAuthentication(server))
            throw new InvalidOperationException("Custom MCP integrations must define OAuth or credential fields before they can be added.");

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
        await _resourceLogWriterService
            .ForAgent(agentId)
            .InfoAsync("Assigned integration {Integration}", server.Name, ct);
    }

    public Task UnassignFromAgentAsync(Guid agentId, string integrationName, CancellationToken ct = default)
        => _agentIntegrationRepository.UnassignAsync(agentId, integrationName, ct);

    public async Task SaveCredentialAsync(Guid ownerId, Guid workspaceId, string integrationName, Dictionary<string, string> fields, CancellationToken ct = default)
    {
        await RequireWorkspaceEditorAsync(ownerId, workspaceId, ct);

        var server = await FindCatalogEntryAsync(ownerId, workspaceId, integrationName, ct)
            ?? throw new InvalidOperationException($"integration '{integrationName}' was not found.");
        if (!RequiresAuthentication(server))
            throw new InvalidOperationException($"integration '{integrationName}' does not declare an authentication method.");

        ValidateCredentialFields(server, fields);
        var encrypted = await _integrationCredentialEncryptionService.ProtectAsync(fields, ct);
        var now = DateTime.UtcNow;
        await _integrationCredentialRepository.UpsertAsync(new IntegrationCredentialRecord
        {
            OwnerId = ownerId,
            WorkspaceId = workspaceId,
            IntegrationName = integrationName,
            AuthKind = InferAuthKind(fields),
            State = IntegrationCredentialState.Active,
            EncryptedSecretEnvelope = encrypted,
            ValidatedAt = now,
            CreatedBy = ownerId,
            ConfiguredAt = now,
            UpdatedAt = now,
        }, ct);
    }

    public async Task SaveOAuthCredentialAsync(
        Guid ownerId,
        Guid workspaceId,
        string provider,
        Dictionary<string, string> fields,
        IReadOnlyList<string> scopes,
        string? email,
        DateTime? expiresAtUtc,
        CancellationToken ct = default)
    {
        await RequireWorkspaceEditorAsync(ownerId, workspaceId, ct);

        var catalog = await OrderedCatalogAsync(ownerId, workspaceId, ct);
        var matching = catalog
            .Where(integration => string.Equals(integration.OauthProvider, provider, StringComparison.OrdinalIgnoreCase))
            .ToList();
        if (matching.Count == 0)
            return;

        var encrypted = await _integrationCredentialEncryptionService.ProtectAsync(fields, ct);
        var now = DateTime.UtcNow;
        var metadata = JsonSerializer.Serialize(new
        {
            provider,
            email,
        });
        var scopesJson = JsonSerializer.Serialize(scopes);
        foreach (var integration in matching)
        {
            await _integrationCredentialRepository.UpsertAsync(new IntegrationCredentialRecord
            {
                OwnerId = ownerId,
                WorkspaceId = workspaceId,
                IntegrationName = integration.Name,
                AuthKind = IntegrationCredentialAuthKinds.OAuth,
                State = IntegrationCredentialState.Active,
                EncryptedSecretEnvelope = encrypted,
                PublicAuthMetadataJson = metadata,
                ScopesJson = scopesJson,
                ExpiresAtUtc = expiresAtUtc,
                ValidatedAt = now,
                CreatedBy = ownerId,
                ConfiguredAt = now,
                UpdatedAt = now,
            }, ct);
        }
    }

    public async Task ArchiveCredentialAsync(Guid ownerId, Guid workspaceId, string integrationName, CancellationToken ct = default)
    {
        await RequireWorkspaceEditorAsync(ownerId, workspaceId, ct);
        await _integrationCredentialRepository.ArchiveAsync(workspaceId, integrationName, ct);
        await _agentIntegrationRepository.UnassignIntegrationFromOwnerAgentsAsync(ownerId, integrationName, ct);
    }

    public async Task<Dictionary<string, string>> GetDecryptedCredentialAsync(string integrationName, Guid? ownerId = null, Guid? workspaceId = null, CancellationToken ct = default)
    {
        if (!ownerId.HasValue || !workspaceId.HasValue) return new();

        var record = await _integrationCredentialRepository.GetByAsync(new IntegrationCredentialFilter
        {
            OwnerId = ownerId.Value,
            WorkspaceId = workspaceId.Value,
            IntegrationName = integrationName,
        }, ct);
        if (record is null) return new();
        await _integrationCredentialRepository.MarkUsedAsync(record.Id, DateTime.UtcNow, ct);
        return await _integrationCredentialEncryptionService.UnprotectAsync(record.EncryptedSecretEnvelope, ct);
    }

    private async Task<IReadOnlyList<IntegrationDefinitionRecord>> WithConnectionStatusAsync(
        Guid ownerId,
        Guid? workspaceId,
        IReadOnlyList<IntegrationDefinitionRecord> servers,
        CancellationToken ct)
    {
        if (!workspaceId.HasValue)
            return servers.Where(RequiresAuthentication).ToList();

        var credentials = await _integrationCredentialRepository.ListAsync(new IntegrationCredentialFilter
        {
            OwnerId = ownerId,
            WorkspaceId = workspaceId.Value,
        }, ct);
        var configured = credentials
            .Where(credential => !string.IsNullOrWhiteSpace(credential.EncryptedSecretEnvelope))
            .ToDictionary(credential => credential.IntegrationName, StringComparer.OrdinalIgnoreCase);

        return servers
            .Where(RequiresAuthentication)
            .Select(server =>
            {
                var isConfigured = configured.ContainsKey(server.Name);
                return CopyWithCredentialConfigured(
                    CopyWithOauthConfigured(server, isConfigured && !string.IsNullOrWhiteSpace(server.OauthProvider)),
                    isConfigured);
            })
            .ToList();
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
        Tools = server.Tools,
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
        Tools = server.Tools,
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
        Tools = server.Tools,
        CapabilitiesJson = server.CapabilitiesJson,
        Entities = server.Entities,
        IsBuiltin = false,
        CreatedAt = server.CreatedAt == default ? DateTime.UtcNow : server.CreatedAt,
    };

    private async Task<IntegrationDefinitionRecord?> FindCatalogEntryAsync(Guid ownerId, Guid workspaceId, string integrationName, CancellationToken ct)
        => (await OrderedCatalogAsync(ownerId, workspaceId, ct))
            .FirstOrDefault(integration => string.Equals(integration.Name, integrationName, StringComparison.OrdinalIgnoreCase));

    private static bool IsConnected(IntegrationDefinitionRecord server)
        => server.CredentialConfigured || server.OauthConfigured;

    private static bool RequiresAuthentication(IntegrationDefinitionRecord server)
        => !string.IsNullOrWhiteSpace(server.OauthProvider) || HasCredentialFields(server);

    private static bool HasCredentialFields(IntegrationDefinitionRecord server)
        => ParseCredentialFields(server.CredentialFieldsJson).Count > 0;

    private static IReadOnlyList<IntegrationCredentialItem> ParseCredentialFields(string? credentialFieldsJson)
    {
        if (string.IsNullOrWhiteSpace(credentialFieldsJson))
            return [];

        try
        {
            var parsed = JsonSerializer.Deserialize<List<IntegrationCredentialItem>>(
                credentialFieldsJson,
                new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
            return parsed?.Where(field => !string.IsNullOrWhiteSpace(field.Name)).ToList() ?? [];
        }
        catch (JsonException)
        {
            return [];
        }
    }

    private static void ValidateCredentialFields(IntegrationDefinitionRecord server, Dictionary<string, string> fields)
    {
        var requiredFields = ParseCredentialFields(server.CredentialFieldsJson)
            .Where(field => field.Required)
            .Select(field => field.Name)
            .ToList();
        foreach (var required in requiredFields)
        {
            if (!fields.TryGetValue(required, out var value) || string.IsNullOrWhiteSpace(value))
                throw new InvalidOperationException($"Credential field '{required}' is required.");
        }
    }

    private static string InferAuthKind(Dictionary<string, string> fields)
    {
        if (fields.Keys.Any(key => key.Contains("BEARER", StringComparison.OrdinalIgnoreCase)
            || key.Contains("TOKEN", StringComparison.OrdinalIgnoreCase)))
            return IntegrationCredentialAuthKinds.Bearer;

        if (fields.Keys.Any(key => key.Contains("CLIENT_SECRET", StringComparison.OrdinalIgnoreCase)))
            return IntegrationCredentialAuthKinds.ClientCredentials;

        return IntegrationCredentialAuthKinds.ApiKey;
    }

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
        if (workspace is null)
            return;

        await _integrationDeploymentRepository.UpsertAsync(new IntegrationDeploymentRecord
        {
            WorkspaceId = workspaceId,
            IntegrationName = integrationName,
            CreatedById = ownerId,
            Enabled = true,
        }, ct);
    }

    private async Task RequireWorkspaceEditorAsync(Guid userId, Guid workspaceId, CancellationToken ct)
    {
        var membership = await _workspaceMemberRepository.GetByAsync(
            new WorkspaceMemberFilter { WorkspaceId = workspaceId, UserId = userId },
            ct);
        if (membership?.Role.CanEdit() == true)
            return;

        throw new InvalidOperationException("Only workspace editors may manage integrations.");
    }

    private static IReadOnlyList<IntegrationDefinitionRecord> OrderedBuiltins()
        => IntegrationDefinitionProvider.BuiltinDefinitions
            .OrderBy(s => s.Category)
            .ThenBy(s => s.Title)
            .ToList();

    private sealed record IntegrationCredentialItem(string Name, bool Required);
}
