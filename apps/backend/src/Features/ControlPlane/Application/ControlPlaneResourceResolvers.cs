using OffceOs.Features.AgentRoutines.Application;
using OffceOs.Features.AgentRoutines.Domain;
using OffceOs.Features.Agents.Application;
using OffceOs.Features.Agents.Domain;
using OffceOs.Features.Browser.Application;
using OffceOs.Features.Browser.Domain;
using OffceOs.Features.Channels.Domain;
using OffceOs.Features.Context.Domain;
using OffceOs.Features.ControlPlane.Domain;
using OffceOs.Features.Integrations.Domain;
using OffceOs.Features.Providers.Application;
using OffceOs.Features.Providers.Domain;
using OffceOs.Features.ResourceLogs.Application;
using OffceOs.Features.ResourceLogs.Domain;

namespace OffceOs.Features.ControlPlane.Application;

internal abstract class ControlPlaneResourceResolver : IControlPlaneResourceResolver
{
    public abstract string Kind { get; }

    public abstract Task<IReadOnlyList<ControlPlaneResourceRecord>> ListAsync(
        ControlPlaneResourceScope scope,
        CancellationToken ct = default);

    public abstract Task<ControlPlaneResourceRecord?> DescribeAsync(
        ControlPlaneResourceScope scope,
        string name,
        CancellationToken ct = default);

    protected static ControlPlaneResourceRecord Resource(
        string kind,
        string name,
        Guid id,
        params (string Key, object? Value)[] fields) =>
        Resource(kind, name, id.ToString(), fields);

    protected static ControlPlaneResourceRecord Resource(
        string kind,
        string name,
        string id,
        params (string Key, object? Value)[] fields)
    {
        var values = new Dictionary<string, object?>(StringComparer.OrdinalIgnoreCase)
        {
            ["kind"] = kind,
            ["name"] = name,
            ["id"] = id,
        };

        foreach (var (key, value) in fields)
            values[key] = value;

        return new ControlPlaneResourceRecord(kind, name, id, values);
    }

}

internal sealed class AgentControlPlaneResourceResolver : ControlPlaneResourceResolver, IDeletableControlPlaneResourceResolver, IMessageControlPlaneResourceResolver
{
    private readonly IAgentRepository _agentRepository;
    private readonly IAgentLifecycleService _agentLifecycleService;
    private readonly IAgentService _agentService;
    private readonly IResourceLogService _resourceLogService;

    public AgentControlPlaneResourceResolver(
        IAgentRepository agentRepository,
        IAgentLifecycleService agentLifecycleService,
        IAgentService agentService,
        IResourceLogService resourceLogService)
    {
        _agentRepository = agentRepository;
        _agentLifecycleService = agentLifecycleService;
        _agentService = agentService;
        _resourceLogService = resourceLogService;
    }

    public override string Kind => "agents";

    public override async Task<IReadOnlyList<ControlPlaneResourceRecord>> ListAsync(
        ControlPlaneResourceScope scope,
        CancellationToken ct = default)
    {
        var agents = await _agentRepository.ListAsync(new AgentFilter { WorkspaceId = scope.WorkspaceId }, ct);
        var logs = await _resourceLogService.ListAsync(new ResourceLogQueryRequest(
            WorkspaceId: scope.WorkspaceId,
            Type: ResourceLogType.MessageIn,
            WorkStatus: string.Empty,
            Limit: 1000), ct);

        return agents
            .Select(agent => ToResource(agent, AgentHealthProjection.From(agent, logs.Items), false))
            .ToArray();
    }

    public override async Task<ControlPlaneResourceRecord?> DescribeAsync(
        ControlPlaneResourceScope scope,
        string name,
        CancellationToken ct = default)
    {
        var agent = await FindAgentAsync(name, scope.WorkspaceId, ct);
        if (agent is null)
            return null;

        var logs = await _resourceLogService.ListAsync(new ResourceLogQueryRequest(
            WorkspaceId: scope.WorkspaceId,
            AgentId: agent.Id,
            Type: ResourceLogType.MessageIn,
            Limit: 1000), ct);

        return ToResource(agent, AgentHealthProjection.From(agent, logs.Items), true);
    }

    public async Task<bool> DeleteAsync(ControlPlaneResourceScope scope, string name, CancellationToken ct = default)
    {
        var agent = await FindAgentAsync(name, scope.WorkspaceId, ct);
        return agent is not null && await _agentLifecycleService.DeleteAsync(agent.Id, scope.UserId, scope.WorkspaceId, ct);
    }

    public async Task<ControlPlaneMessageResult> SendMessageAsync(
        ControlPlaneResourceScope scope,
        string name,
        ControlPlaneMessageRequest request,
        CancellationToken ct = default)
    {
        var agent = await FindAgentAsync(name, scope.WorkspaceId, ct);
        if (agent is null)
            return ControlPlaneMessageResult.NotFoundResult($"{Kind}/{name}");

        if (string.IsNullOrWhiteSpace(request.Message))
            return ControlPlaneMessageResult.BadRequest("Message is required.");

        var work = await _agentService.SendMessageAsync(
            agent.Id,
            request.Message.Trim(),
            scope.UserId,
            ct,
            string.IsNullOrWhiteSpace(request.Purpose) ? AgentWorkPurposeKinds.Manual : request.Purpose.Trim(),
            agent.ActiveDefinitionId);

        return ControlPlaneMessageResult.Sent(new
        {
            kind = "AgentWork",
            agentId = agent.Id,
            agentName = agent.Name,
            workLogId = work.Id,
            correlationId = work.CorrelationId,
            status = work.WorkStatus,
            purpose = work.WorkPurpose,
            createdAt = work.Time,
        });
    }

    private async Task<AgentRecord?> FindAgentAsync(string name, Guid workspaceId, CancellationToken ct)
    {
        if (Guid.TryParse(name, out var id))
            return await _agentRepository.GetByAsync(new AgentFilter { Id = id, WorkspaceId = workspaceId }, ct);

        var matches = await _agentRepository.ListAsync(new AgentFilter { WorkspaceId = workspaceId }, ct);
        var match = matches.FirstOrDefault(agent => agent.Name.Equals(name, StringComparison.OrdinalIgnoreCase));
        return match is null
            ? null
            : await _agentRepository.GetByAsync(new AgentFilter { Id = match.Id, WorkspaceId = workspaceId }, ct);
    }

    private static ControlPlaneResourceRecord ToResource(AgentRecord agent, AgentHealthResult health, bool detailed)
    {
        var fields = new List<(string Key, object? Value)>
        {
            ("provider", agent.Provider),
            ("model", agent.Model),
            ("status", health.Status),
            ("rawStatus", agent.Status.ToString()),
            ("health", health),
            ("createdAt", agent.CreatedAt),
        };

        if (detailed)
        {
            fields.AddRange(
            [
                ("prompt", agent.Prompt),
                ("systemPrompt", SystemPromptComposer.Compose(agent)),
                ("activeDefinitionId", agent.ActiveDefinitionId),
                ("workspaceId", agent.WorkspaceId),
                ("personalityFiles", agent.PersonalityFiles.OrderBy(file => file.CompositionOrder).Select(file => new
                {
                    file.FileName,
                    file.Content,
                    file.CreatedAt,
                    file.UpdatedAt,
                })),
                ("memories", agent.Memories.Select(memory => new
                {
                    memory.Key,
                    memory.Content,
                    memory.CreatedAt,
                    memory.UpdatedAt,
                })),
                ("channelBindings", agent.ChannelBindings.Select(binding => new
                {
                    binding.Id,
                    binding.ChannelConnectionId,
                    binding.Enabled,
                    binding.Config,
                    binding.CreatedAt,
                })),
                ("activeSession", agent.ActiveSession is null ? null : new
                {
                    agent.ActiveSession.Id,
                    status = agent.ActiveSession.Status.ToString(),
                    agent.ActiveSession.Source,
                    agent.ActiveSession.Purpose,
                    agent.ActiveSession.LastActivityAt,
                    agent.ActiveSession.CreatedAt,
                    agent.ActiveSession.CompletedAt,
                }),
            ]);
        }

        return Resource(ResourceLogKinds.Agent, agent.Name, agent.Id, fields.ToArray());
    }
}

internal sealed class BrowserControlPlaneResourceResolver : ControlPlaneResourceResolver, IDeletableControlPlaneResourceResolver
{
    private readonly IBrowserResourceService _browserResourceService;

    public BrowserControlPlaneResourceResolver(IBrowserResourceService browserResourceService)
    {
        _browserResourceService = browserResourceService;
    }

    public override string Kind => "browsers";

    public override async Task<IReadOnlyList<ControlPlaneResourceRecord>> ListAsync(
        ControlPlaneResourceScope scope,
        CancellationToken ct = default) =>
        (await _browserResourceService.ListAsync(scope.WorkspaceId, ct)).Select(ToResource).ToArray();

    public override async Task<ControlPlaneResourceRecord?> DescribeAsync(
        ControlPlaneResourceScope scope,
        string name,
        CancellationToken ct = default)
    {
        var browser = await FindBrowserAsync(scope, name, ct);
        return browser is null ? null : ToResource(browser);
    }

    public async Task<bool> DeleteAsync(ControlPlaneResourceScope scope, string name, CancellationToken ct = default)
    {
        var browser = await FindBrowserAsync(scope, name, ct);
        return browser is not null && await _browserResourceService.DeleteAsync(browser.Id, scope.WorkspaceId, ct);
    }

    private async Task<BrowserResourceRecord?> FindBrowserAsync(
        ControlPlaneResourceScope scope,
        string name,
        CancellationToken ct)
    {
        if (Guid.TryParse(name, out var id))
            return await _browserResourceService.GetAsync(id, scope.WorkspaceId, ct);

        var browsers = await _browserResourceService.ListAsync(scope.WorkspaceId, ct);
        return browsers.FirstOrDefault(browser => browser.DisplayName.Equals(name, StringComparison.OrdinalIgnoreCase));
    }

    private static ControlPlaneResourceRecord ToResource(BrowserResourceRecord browser) => Resource(
        ResourceLogKinds.Browser,
        browser.DisplayName,
        browser.Id,
        ("displayName", browser.DisplayName),
        ("currentAgentId", browser.CurrentAgentId),
        ("createdAt", browser.CreatedAt),
        ("updatedAt", browser.UpdatedAt));
}

internal sealed class ChannelControlPlaneResourceResolver : ControlPlaneResourceResolver, IDeletableControlPlaneResourceResolver
{
    private readonly IChannelRepository _channelRepository;

    public ChannelControlPlaneResourceResolver(IChannelRepository channelRepository)
    {
        _channelRepository = channelRepository;
    }

    public override string Kind => "channels";

    public override async Task<IReadOnlyList<ControlPlaneResourceRecord>> ListAsync(
        ControlPlaneResourceScope scope,
        CancellationToken ct = default) =>
        (await _channelRepository.ListConnectionsAsync(new ChannelConnectionFilter { WorkspaceId = scope.WorkspaceId }, ct))
        .Select(ToResource)
        .ToArray();

    public override async Task<ControlPlaneResourceRecord?> DescribeAsync(
        ControlPlaneResourceScope scope,
        string name,
        CancellationToken ct = default)
    {
        var channel = await FindChannelAsync(scope.WorkspaceId, name, ct);
        return channel is null ? null : ToResource(channel);
    }

    public async Task<bool> DeleteAsync(ControlPlaneResourceScope scope, string name, CancellationToken ct = default)
    {
        var channel = await FindChannelAsync(scope.WorkspaceId, name, ct);
        return channel is not null && await _channelRepository.DeleteConnectionAsync(channel.Id, ct);
    }

    private async Task<ChannelConnectionRecord?> FindChannelAsync(Guid workspaceId, string name, CancellationToken ct)
    {
        if (Guid.TryParse(name, out var id))
            return await _channelRepository.GetConnectionByAsync(new ChannelConnectionFilter { Id = id, WorkspaceId = workspaceId }, ct);

        var channels = await _channelRepository.ListConnectionsAsync(new ChannelConnectionFilter { WorkspaceId = workspaceId }, ct);
        return channels.FirstOrDefault(channel => channel.DisplayName.Equals(name, StringComparison.OrdinalIgnoreCase));
    }

    private static ControlPlaneResourceRecord ToResource(ChannelConnectionRecord channel) => Resource(
        ResourceLogKinds.Channel,
        channel.DisplayName,
        channel.Id,
        ("type", channel.ChannelType.ToStorageString()),
        ("platform", channel.ChannelType.ToStorageString()),
        ("displayName", channel.DisplayName),
        ("enabled", channel.Enabled),
        ("createdAt", channel.CreatedAt));
}

internal sealed class ControlPlaneControlPlaneResourceResolver : ControlPlaneResourceResolver
{
    public override string Kind => "control-plane";

    public override Task<IReadOnlyList<ControlPlaneResourceRecord>> ListAsync(
        ControlPlaneResourceScope scope,
        CancellationToken ct = default) =>
        Task.FromResult<IReadOnlyList<ControlPlaneResourceRecord>>([ToResource(scope)]);

    public override Task<ControlPlaneResourceRecord?> DescribeAsync(
        ControlPlaneResourceScope scope,
        string name,
        CancellationToken ct = default)
    {
        var resource = name.Equals("control-plane", StringComparison.OrdinalIgnoreCase)
            || name.Equals("controlplane", StringComparison.OrdinalIgnoreCase)
            || name.Equals("system", StringComparison.OrdinalIgnoreCase)
            || name.Equals(scope.WorkspaceId.ToString(), StringComparison.OrdinalIgnoreCase)
            ? ToResource(scope)
            : null;

        return Task.FromResult(resource);
    }

    private static ControlPlaneResourceRecord ToResource(ControlPlaneResourceScope scope) => Resource(
        ResourceLogKinds.ControlPlane,
        "control-plane",
        scope.WorkspaceId,
        ("workspaceId", scope.WorkspaceId));
}

internal sealed class CredentialControlPlaneResourceResolver : ControlPlaneResourceResolver, IDeletableControlPlaneResourceResolver
{
    private readonly IAgentRoutineCredentialRepository _agentRoutineCredentialRepository;

    public CredentialControlPlaneResourceResolver(IAgentRoutineCredentialRepository agentRoutineCredentialRepository)
    {
        _agentRoutineCredentialRepository = agentRoutineCredentialRepository;
    }

    public override string Kind => "credentials";

    public override async Task<IReadOnlyList<ControlPlaneResourceRecord>> ListAsync(
        ControlPlaneResourceScope scope,
        CancellationToken ct = default) =>
        (await _agentRoutineCredentialRepository.ListAsync(scope.WorkspaceId, ct)).Select(ToResource).ToArray();

    public override async Task<ControlPlaneResourceRecord?> DescribeAsync(
        ControlPlaneResourceScope scope,
        string name,
        CancellationToken ct = default)
    {
        var credential = await FindCredentialAsync(scope.WorkspaceId, name, ct);
        return credential is null ? null : ToResource(credential);
    }

    public async Task<bool> DeleteAsync(ControlPlaneResourceScope scope, string name, CancellationToken ct = default)
    {
        var credential = await FindCredentialAsync(scope.WorkspaceId, name, ct);
        return credential is not null && await _agentRoutineCredentialRepository.DeleteAsync(scope.WorkspaceId, credential.Name, ct);
    }

    private async Task<AgentRoutineCredentialRecord?> FindCredentialAsync(Guid workspaceId, string name, CancellationToken ct)
    {
        if (!Guid.TryParse(name, out var id))
            return await _agentRoutineCredentialRepository.GetByNameAsync(workspaceId, name, ct);

        var credentials = await _agentRoutineCredentialRepository.ListAsync(workspaceId, ct);
        return credentials.FirstOrDefault(credential => credential.Id == id);
    }

    private static ControlPlaneResourceRecord ToResource(AgentRoutineCredentialRecord credential) => Resource(
        ResourceLogKinds.Credential,
        credential.Name,
        credential.Id,
        ("provider", credential.Provider),
        ("authKind", credential.AuthKind),
        ("configured", !string.IsNullOrWhiteSpace(credential.EncryptedSecret)),
        ("expiresAtUtc", credential.ExpiresAtUtc),
        ("lastUsedAt", credential.LastUsedAt),
        ("createdAt", credential.CreatedAt),
        ("updatedAt", credential.UpdatedAt));
}

internal sealed class IntegrationControlPlaneResourceResolver : ControlPlaneResourceResolver, IDeletableControlPlaneResourceResolver
{
    private readonly IIntegrationDeploymentRepository _integrationDeploymentRepository;

    public IntegrationControlPlaneResourceResolver(IIntegrationDeploymentRepository integrationDeploymentRepository)
    {
        _integrationDeploymentRepository = integrationDeploymentRepository;
    }

    public override string Kind => "integrations";

    public override async Task<IReadOnlyList<ControlPlaneResourceRecord>> ListAsync(
        ControlPlaneResourceScope scope,
        CancellationToken ct = default) =>
        (await _integrationDeploymentRepository.ListAsync(new IntegrationDeploymentFilter { WorkspaceId = scope.WorkspaceId }, ct))
        .Select(ToResource)
        .ToArray();

    public override async Task<ControlPlaneResourceRecord?> DescribeAsync(
        ControlPlaneResourceScope scope,
        string name,
        CancellationToken ct = default)
    {
        var deployment = await FindIntegrationAsync(scope.WorkspaceId, name, ct);
        return deployment is null ? null : ToResource(deployment);
    }

    public async Task<bool> DeleteAsync(ControlPlaneResourceScope scope, string name, CancellationToken ct = default)
    {
        var deployment = await FindIntegrationAsync(scope.WorkspaceId, name, ct);
        return deployment is not null && await _integrationDeploymentRepository.DeleteAsync(new IntegrationDeploymentFilter
        {
            WorkspaceId = scope.WorkspaceId,
            IntegrationName = deployment.IntegrationName,
        }, ct);
    }

    private async Task<IntegrationDeploymentRecord?> FindIntegrationAsync(Guid workspaceId, string name, CancellationToken ct)
    {
        if (Guid.TryParse(name, out var id))
            return await _integrationDeploymentRepository.GetByAsync(new IntegrationDeploymentFilter { Id = id, WorkspaceId = workspaceId }, ct);

        return await _integrationDeploymentRepository.GetByAsync(new IntegrationDeploymentFilter
        {
            WorkspaceId = workspaceId,
            IntegrationName = name,
        }, ct);
    }

    private static ControlPlaneResourceRecord ToResource(IntegrationDeploymentRecord deployment) => Resource(
        ResourceLogKinds.IntegrationDeployment,
        deployment.IntegrationName,
        deployment.Id,
        ("workspaceId", deployment.WorkspaceId),
        ("enabled", deployment.Enabled),
        ("server", deployment.IntegrationName),
        ("status", deployment.Enabled ? "enabled" : "disabled"),
        ("createdAt", deployment.CreatedAt),
        ("updatedAt", deployment.UpdatedAt));
}

internal sealed class MemoryStoreControlPlaneResourceResolver : ControlPlaneResourceResolver, IDeletableControlPlaneResourceResolver
{
    private readonly IMemoryStoreRepository _memoryStoreRepository;

    public MemoryStoreControlPlaneResourceResolver(IMemoryStoreRepository memoryStoreRepository)
    {
        _memoryStoreRepository = memoryStoreRepository;
    }

    public override string Kind => "memory-stores";

    public override async Task<IReadOnlyList<ControlPlaneResourceRecord>> ListAsync(
        ControlPlaneResourceScope scope,
        CancellationToken ct = default) =>
        (await _memoryStoreRepository.ListAsync(null, scope.WorkspaceId, ct)).Select(ToResource).ToArray();

    public override async Task<ControlPlaneResourceRecord?> DescribeAsync(
        ControlPlaneResourceScope scope,
        string name,
        CancellationToken ct = default)
    {
        var store = await FindMemoryStoreAsync(scope.WorkspaceId, name, ct);
        return store is null ? null : ToResource(store);
    }

    public async Task<bool> DeleteAsync(ControlPlaneResourceScope scope, string name, CancellationToken ct = default)
    {
        var store = await FindMemoryStoreAsync(scope.WorkspaceId, name, ct);
        return store is not null && await _memoryStoreRepository.DeleteAsync(store.Id, null, scope.WorkspaceId, ct);
    }

    private async Task<MemoryStoreRecord?> FindMemoryStoreAsync(Guid workspaceId, string name, CancellationToken ct)
    {
        if (Guid.TryParse(name, out var id))
            return await _memoryStoreRepository.GetAsync(id, null, workspaceId, ct);

        var stores = await _memoryStoreRepository.ListAsync(null, workspaceId, ct);
        return stores.FirstOrDefault(store => store.DisplayName.Equals(name, StringComparison.OrdinalIgnoreCase));
    }

    private static ControlPlaneResourceRecord ToResource(MemoryStoreRecord store) => Resource(
        ResourceLogKinds.MemoryStore,
        store.DisplayName,
        store.Id,
        ("displayName", store.DisplayName),
        ("entryCount", null),
        ("createdAt", store.CreatedAt),
        ("updatedAt", store.UpdatedAt));
}

internal sealed class ModelControlPlaneResourceResolver : ControlPlaneResourceResolver
{
    private readonly IProviderService _providerService;

    public ModelControlPlaneResourceResolver(IProviderService providerService)
    {
        _providerService = providerService;
    }

    public override string Kind => "models";

    public override async Task<IReadOnlyList<ControlPlaneResourceRecord>> ListAsync(
        ControlPlaneResourceScope scope,
        CancellationToken ct = default)
    {
        var providers = await _providerService.ListForWorkspaceAsync(scope.WorkspaceId, ct);
        return providers
            .SelectMany(provider => provider.Models.Select(model => ToResource(provider, model)))
            .ToArray();
    }

    public override async Task<ControlPlaneResourceRecord?> DescribeAsync(
        ControlPlaneResourceScope scope,
        string name,
        CancellationToken ct = default)
    {
        var providers = await _providerService.ListForWorkspaceAsync(scope.WorkspaceId, ct);
        return providers
            .SelectMany(provider => provider.Models.Select(model => ToResource(provider, model)))
            .FirstOrDefault(model =>
                model.Name.Equals(name, StringComparison.OrdinalIgnoreCase)
                || model.Id.Equals(name, StringComparison.OrdinalIgnoreCase)
                || model.Id.Equals(name.Replace('/', ':'), StringComparison.OrdinalIgnoreCase)
                || string.Equals(model.Fields.GetValueOrDefault("displayName")?.ToString(), name, StringComparison.OrdinalIgnoreCase));
    }

    private static ControlPlaneResourceRecord ToResource(ProviderResult provider, ProviderModelResult model) => Resource(
        ResourceLogKinds.Model,
        model.Id,
        $"{provider.Name}:{model.Id}",
        ("provider", provider.Name),
        ("displayName", model.DisplayName),
        ("costWeight", model.CostWeight),
        ("configured", provider.Configured));
}

internal sealed class ProviderControlPlaneResourceResolver : ControlPlaneResourceResolver, IDeletableControlPlaneResourceResolver, IAuthenticatableControlPlaneResourceResolver
{
    private readonly IProviderResourceRepository _providerResourceRepository;
    private readonly IProviderService _providerService;

    public ProviderControlPlaneResourceResolver(
        IProviderResourceRepository providerResourceRepository,
        IProviderService providerService)
    {
        _providerResourceRepository = providerResourceRepository;
        _providerService = providerService;
    }

    public override string Kind => "providers";

    public override async Task<IReadOnlyList<ControlPlaneResourceRecord>> ListAsync(
        ControlPlaneResourceScope scope,
        CancellationToken ct = default) =>
        (await _providerResourceRepository.ListAsync(scope.WorkspaceId, ct)).Select(ToResource).ToArray();

    public override async Task<ControlPlaneResourceRecord?> DescribeAsync(
        ControlPlaneResourceScope scope,
        string name,
        CancellationToken ct = default)
    {
        var provider = await FindProviderAsync(scope.WorkspaceId, name, ct);
        return provider is null ? null : ToResource(provider);
    }

    public async Task<bool> DeleteAsync(ControlPlaneResourceScope scope, string name, CancellationToken ct = default)
    {
        var provider = await FindProviderAsync(scope.WorkspaceId, name, ct);
        return provider is not null && await _providerResourceRepository.DeleteAsync(scope.WorkspaceId, provider.Name, ct);
    }

    public async Task<ControlPlaneAuthenticationResult> AuthenticateAsync(
        ControlPlaneResourceScope scope,
        string name,
        ControlPlaneAuthenticationRequest request,
        CancellationToken ct = default)
    {
        if (!name.Equals(ProviderRegistry.CodexProviderSlug, StringComparison.OrdinalIgnoreCase))
        {
            return ControlPlaneAuthenticationResult.UnsupportedResult($"{Kind}/{name}");
        }

        if (string.IsNullOrWhiteSpace(request.AccessToken))
            return ControlPlaneAuthenticationResult.BadRequest("Codex access token is required.");
        if (string.IsNullOrWhiteSpace(request.RefreshToken))
            return ControlPlaneAuthenticationResult.BadRequest("Codex refresh token is required.");

        var result = await _providerService.AuthenticateCodexAsync(scope.WorkspaceId, new CodexProviderAuthRequest(
            request.AccessToken,
            request.RefreshToken,
            request.ExpiresAt,
            request.AccountEmail,
            request.AccountId,
            request.ClientId,
            request.TokenUrl,
            request.Scopes), ct);

        return ControlPlaneAuthenticationResult.Authenticated(result);
    }

    private async Task<ProviderResourceRecord?> FindProviderAsync(Guid workspaceId, string name, CancellationToken ct)
    {
        if (!Guid.TryParse(name, out var id))
            return await _providerResourceRepository.GetByNameAsync(workspaceId, name, ct);

        var providers = await _providerResourceRepository.ListAsync(workspaceId, ct);
        return providers.FirstOrDefault(provider => provider.Id == id);
    }

    private static ControlPlaneResourceRecord ToResource(ProviderResourceRecord provider) => Resource(
        ResourceLogKinds.Provider,
        provider.Name,
        provider.Id,
        ("type", provider.Type),
        ("displayName", provider.DisplayName),
        ("enabled", provider.Enabled),
        ("configured", provider.Enabled && !string.IsNullOrWhiteSpace(provider.EncryptedCredentialsJson)),
        ("phase", provider.Phase),
        ("statusMessage", provider.StatusMessage),
        ("account", provider.Account),
        ("expiresAt", provider.ExpiresAt),
        ("lastValidatedAt", provider.LastValidatedAt),
        ("defaultModel", provider.DefaultModel),
        ("models", provider.Models),
        ("createdAt", provider.CreatedAt),
        ("updatedAt", provider.UpdatedAt));
}

internal sealed class RoutineControlPlaneResourceResolver : ControlPlaneResourceResolver, IDeletableControlPlaneResourceResolver
{
    private readonly IAgentRoutineService _agentRoutineService;

    public RoutineControlPlaneResourceResolver(IAgentRoutineService agentRoutineService)
    {
        _agentRoutineService = agentRoutineService;
    }

    public override string Kind => "routines";

    public override async Task<IReadOnlyList<ControlPlaneResourceRecord>> ListAsync(
        ControlPlaneResourceScope scope,
        CancellationToken ct = default) =>
        (await _agentRoutineService.ListForOwnerAsync(scope.UserId, scope.WorkspaceId, ct)).Select(ToResource).ToArray();

    public override async Task<ControlPlaneResourceRecord?> DescribeAsync(
        ControlPlaneResourceScope scope,
        string name,
        CancellationToken ct = default)
    {
        var routine = await FindRoutineAsync(scope, name, ct);
        return routine is null ? null : ToResource(routine);
    }

    public async Task<bool> DeleteAsync(ControlPlaneResourceScope scope, string name, CancellationToken ct = default)
    {
        var routine = await FindRoutineAsync(scope, name, ct);
        return routine is not null && await _agentRoutineService.DeleteAsync(routine.Routine.Id, scope.UserId, scope.WorkspaceId, ct);
    }

    private async Task<AgentRoutineWithAgentRecord?> FindRoutineAsync(
        ControlPlaneResourceScope scope,
        string name,
        CancellationToken ct)
    {
        if (Guid.TryParse(name, out var id))
            return await _agentRoutineService.GetForOwnerAsync(id, scope.UserId, scope.WorkspaceId, ct);

        var routines = await _agentRoutineService.ListForOwnerAsync(scope.UserId, scope.WorkspaceId, ct);
        return routines.FirstOrDefault(routine => routine.Routine.Name.Equals(name, StringComparison.OrdinalIgnoreCase));
    }

    private static ControlPlaneResourceRecord ToResource(AgentRoutineWithAgentRecord routine) => Resource(
        ResourceLogKinds.Routine,
        routine.Routine.Name,
        routine.Routine.Id,
        ("agentId", routine.Routine.AgentId),
        ("agentName", routine.AgentName),
        ("enabled", routine.Routine.Enabled),
        ("prompt", routine.Routine.Prompt),
        ("lastTriggeredAt", routine.Routine.LastTriggeredAt),
        ("createdAt", routine.Routine.CreatedAt),
        ("triggers", routine.Routine.Triggers.Select(trigger => new
        {
            trigger.Id,
            trigger.Kind,
            trigger.Name,
            trigger.Enabled,
            trigger.LastTriggeredAt,
            trigger.NextRunAt,
            trigger.CreatedAt,
        })));
}
