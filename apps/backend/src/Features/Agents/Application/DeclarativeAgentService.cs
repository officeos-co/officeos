namespace OffceOs.Application.Features.Agents;

internal sealed class DeclarativeAgentService : IDeclarativeAgentService
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
    };

    private static readonly string[] ApplyOrder =
    [
        DeclarativeResourceKindItem.Integration,
        DeclarativeResourceKindItem.Channel,
        DeclarativeResourceKindItem.MemoryStore,
        DeclarativeResourceKindItem.Browser,
        DeclarativeResourceKindItem.Agent,
        DeclarativeResourceKindItem.Routine,
    ];

    private readonly IAgentDashboardService _agentDashboardService;
    private readonly IAgentRepository _agentRepository;
    private readonly IAgentDefinitionRepository _agentDefinitionRepository;
    private readonly IAgentSessionRepository _agentSessionRepository;
    private readonly IAgentResourceRepository _agentResourceRepository;
    private readonly IChannelRepository _channelRepository;
    private readonly IChannelService _channelService;
    private readonly IIntegrationDefinitionRepository _integrationDefinitionRepository;
    private readonly IIntegrationDefinitionService _integrationDefinitionService;
    private readonly IMemoryStoreRepository _memoryStoreRepository;
    private readonly IAgentRoutineService _agentRoutineService;
    private readonly AgentDefinitionParser _agentDefinitionParser;
    private readonly DeclarativeManifestParser _declarativeManifestParser;
    private readonly ChannelCredentialProtector _channelCredentialProtector;

    public DeclarativeAgentService(
        IAgentDashboardService agentDashboardService,
        IAgentRepository agentRepository,
        IAgentDefinitionRepository agentDefinitionRepository,
        IAgentSessionRepository agentSessionRepository,
        IAgentResourceRepository agentResourceRepository,
        IChannelRepository channelRepository,
        IChannelService channelService,
        IIntegrationDefinitionRepository integrationDefinitionRepository,
        IIntegrationDefinitionService integrationDefinitionService,
        IMemoryStoreRepository memoryStoreRepository,
        IAgentRoutineService agentRoutineService,
        AgentDefinitionParser agentDefinitionParser,
        DeclarativeManifestParser declarativeManifestParser,
        ChannelCredentialProtector channelCredentialProtector)
    {
        _agentDashboardService = agentDashboardService;
        _agentRepository = agentRepository;
        _agentDefinitionRepository = agentDefinitionRepository;
        _agentSessionRepository = agentSessionRepository;
        _agentResourceRepository = agentResourceRepository;
        _channelRepository = channelRepository;
        _channelService = channelService;
        _integrationDefinitionRepository = integrationDefinitionRepository;
        _integrationDefinitionService = integrationDefinitionService;
        _memoryStoreRepository = memoryStoreRepository;
        _agentRoutineService = agentRoutineService;
        _agentDefinitionParser = agentDefinitionParser;
        _declarativeManifestParser = declarativeManifestParser;
        _channelCredentialProtector = channelCredentialProtector;
    }

    public async Task<DeclarativeManifestValidationResult> ValidateAsync(string manifest, Guid ownerId, Guid workspaceId, CancellationToken ct = default)
    {
        var validation = await ValidateInternalAsync(manifest, ownerId, workspaceId, ct);
        return new DeclarativeManifestValidationResult(
            validation.Errors.Count == 0,
            validation.Errors,
            validation.Resources.Select(resource => $"{resource.Kind}/{resource.Name}").ToList());
    }

    public async Task<DeclarativeManifestDiffResult> DiffAsync(string manifest, Guid ownerId, Guid workspaceId, CancellationToken ct = default)
    {
        var validation = await RequireValidAsync(manifest, ownerId, workspaceId, ct);
        var changes = new List<DeclarativeResourceChangeItem>();
        foreach (var resource in Ordered(validation.Resources))
            changes.Add(await BuildChangeAsync(resource, ownerId, workspaceId, ct));

        return new DeclarativeManifestDiffResult(changes);
    }

    public async Task<DeclarativeManifestApplyResult> ApplyAsync(string manifest, Guid ownerId, Guid workspaceId, CancellationToken ct = default)
    {
        var validation = await RequireValidAsync(manifest, ownerId, workspaceId, ct);
        var changes = new List<DeclarativeResourceChangeItem>();
        var appliedAgents = new Dictionary<string, AgentRecord>(StringComparer.OrdinalIgnoreCase);

        foreach (var resource in Ordered(validation.Resources))
        {
            switch (resource.Kind)
            {
                case DeclarativeResourceKindItem.Integration:
                    changes.Add(await ApplyIntegrationAsync(resource, ownerId, workspaceId, ct));
                    break;
                case DeclarativeResourceKindItem.Channel:
                    changes.Add(await ApplyChannelAsync(resource, ownerId, workspaceId, ct));
                    break;
                case DeclarativeResourceKindItem.MemoryStore:
                    changes.Add(await ApplyMemoryStoreAsync(resource, ownerId, workspaceId, ct));
                    break;
                case DeclarativeResourceKindItem.Browser:
                    changes.Add(await ApplyBrowserAsync(resource, ownerId, workspaceId, ct));
                    break;
                case DeclarativeResourceKindItem.Agent:
                    var (agent, change) = await ApplyAgentAsync(resource, ownerId, workspaceId, ct);
                    appliedAgents[resource.Name] = agent;
                    changes.Add(change);
                    break;
                case DeclarativeResourceKindItem.Routine:
                    changes.Add(await ApplyRoutineAsync(resource, appliedAgents, ownerId, workspaceId, ct));
                    break;
            }
        }

        return new DeclarativeManifestApplyResult(changes);
    }

    public async Task<string> ExportWorkspaceAsync(Guid ownerId, Guid workspaceId, CancellationToken ct = default)
    {
        var items = new List<DeclarativeResourceItem>();

        var integrations = await _integrationDefinitionRepository.ListAsync(ownerId, workspaceId, ct);
        items.AddRange(integrations.Select(integration => NewItem(
            DeclarativeResourceKindItem.Integration,
            integration.Name,
            new DeclarativeIntegrationSpecItem(
                false,
                integration.Provider,
                integration.Title,
                integration.Description,
                integration.TransportType.ToString(),
                integration.Command,
                integration.Args,
                integration.Url,
                integration.Category,
                integration.Logo,
                integration.CredentialFieldsJson,
                null))));

        var channels = await _channelRepository.ListConnectionsAsync(new ChannelConnectionFilter { WorkspaceId = workspaceId }, ct);
        items.AddRange(channels.Select(channel => NewItem(
            DeclarativeResourceKindItem.Channel,
            channel.Id.ToString(),
            new DeclarativeChannelSpecItem(channel.ChannelType.ToStorageString(), channel.DisplayName, channel.Enabled, null, null))));

        var memoryStores = await _memoryStoreRepository.ListAsync(null, workspaceId, ct);
        foreach (var memoryStore in memoryStores)
        {
            var entries = await _memoryStoreRepository.ListEntriesAsync(memoryStore.Id, null, workspaceId, ct);
            items.Add(NewItem(
                DeclarativeResourceKindItem.MemoryStore,
                memoryStore.Id.ToString(),
                new DeclarativeMemoryStoreSpecItem(
                    memoryStore.DisplayName,
                    entries.Select(entry => new DeclarativeMemoryStoreEntryItem(entry.Key, entry.Content)).ToList())));
        }

        var browsers = await _agentResourceRepository.ListBrowserResourcesAsync(null, workspaceId, ct);
        items.AddRange(browsers.Select(browser => NewItem(
            DeclarativeResourceKindItem.Browser,
            browser.Id.ToString(),
            new DeclarativeBrowserSpecItem(browser.DisplayName))));

        var agents = await _agentRepository.ListAsync(new AgentFilter { WorkspaceId = workspaceId }, ct);
        foreach (var agent in agents)
        {
            var definition = await _agentDefinitionRepository.GetByAsync(new AgentDefinitionFilter { AgentId = agent.Id, ActiveOnly = true }, ct);
            if (definition is null)
                continue;

            items.Add(NewItem(DeclarativeResourceKindItem.Agent, agent.Name, FromDefinition(agent.Provider, _agentDefinitionParser.Parse(definition.ConfigJson))));
        }

        return _declarativeManifestParser.Serialize(new DeclarativeWorkspaceItem(items));
    }

    public async Task<string?> ExportAgentAsync(string name, Guid ownerId, Guid workspaceId, CancellationToken ct = default)
    {
        var agent = await FindAgentByNameAsync(name, workspaceId, ct);
        if (agent is null)
            return null;

        var definition = await _agentDefinitionRepository.GetByAsync(new AgentDefinitionFilter { AgentId = agent.Id, ActiveOnly = true }, ct);
        if (definition is null)
            return null;

        return _declarativeManifestParser.Serialize(new DeclarativeWorkspaceItem(
        [
            NewItem(DeclarativeResourceKindItem.Agent, agent.Name, FromDefinition(agent.Provider, _agentDefinitionParser.Parse(definition.ConfigJson))),
        ]));
    }

    private async Task<DeclarativeValidationItem> ValidateInternalAsync(string manifest, Guid ownerId, Guid workspaceId, CancellationToken ct)
    {
        await Task.CompletedTask;
        var errors = new List<DeclarativeValidationErrorItem>();
        DeclarativeWorkspaceItem config;
        try
        {
            config = _declarativeManifestParser.Parse(manifest);
        }
        catch (InvalidOperationException ex)
        {
            return new DeclarativeValidationItem([], [new DeclarativeValidationErrorItem("Manifest", string.Empty, ex.Message)]);
        }

        var resources = new List<DeclarativeResourceDescriptorItem>();
        foreach (var item in config.Items)
        {
            var kind = NormalizeKind(item.Kind);
            var name = NormalizeName(item.Metadata?.Name);
            if (kind is null)
            {
                errors.Add(new DeclarativeValidationErrorItem(item.Kind, name ?? string.Empty, $"Resource kind '{item.Kind}' is not supported."));
                continue;
            }

            if (name is null)
            {
                errors.Add(new DeclarativeValidationErrorItem(kind, string.Empty, $"{kind} metadata.name is required."));
                continue;
            }

            resources.Add(new DeclarativeResourceDescriptorItem(kind, name, item));
        }

        foreach (var duplicate in resources
            .GroupBy(resource => (resource.Kind, resource.Name), DeclarativeResourceTupleItem.OrdinalIgnoreCase)
            .Where(group => group.Count() > 1))
            errors.Add(new DeclarativeValidationErrorItem(duplicate.Key.Kind, duplicate.Key.Name, "Resource is declared more than once."));

        var resourceNames = resources
            .GroupBy(resource => resource.Kind)
            .ToDictionary(group => group.Key, group => group.Select(resource => resource.Name).ToHashSet(StringComparer.OrdinalIgnoreCase));

        foreach (var resource in resources)
        {
            try
            {
                ValidateResource(resource, resourceNames);
            }
            catch (InvalidOperationException ex)
            {
                errors.Add(new DeclarativeValidationErrorItem(resource.Kind, resource.Name, ex.Message));
            }
        }

        foreach (var resource in resources.Where(resource => resource.Kind == DeclarativeResourceKindItem.Integration))
        {
            var spec = Spec<DeclarativeIntegrationSpecItem>(resource);
            if (spec.Builtin == true && IntegrationDefinitionProvider.GetBuiltin(resource.Name) is null)
                errors.Add(new DeclarativeValidationErrorItem(resource.Kind, resource.Name, $"Builtin integration '{resource.Name}' does not exist."));
            if (spec.Builtin != true && IntegrationDefinitionProvider.GetBuiltin(resource.Name) is not null)
                errors.Add(new DeclarativeValidationErrorItem(resource.Kind, resource.Name, $"Integration '{resource.Name}' is built in; set spec.builtin: true."));
        }

        _ = ownerId;
        _ = workspaceId;
        _ = ct;
        return new DeclarativeValidationItem(resources, errors);
    }

    private async Task<DeclarativeValidationItem> RequireValidAsync(string manifest, Guid ownerId, Guid workspaceId, CancellationToken ct)
    {
        var validation = await ValidateInternalAsync(manifest, ownerId, workspaceId, ct);
        if (validation.Errors.Count > 0)
            throw new InvalidOperationException(string.Join("; ", validation.Errors.Select(error => $"{error.Kind}/{error.Name}: {error.Message}")));

        return validation;
    }

    private static void ValidateResource(DeclarativeResourceDescriptorItem resource, IReadOnlyDictionary<string, HashSet<string>> resourceNames)
    {
        switch (resource.Kind)
        {
            case DeclarativeResourceKindItem.Channel:
                var channel = Spec<DeclarativeChannelSpecItem>(resource);
                if (ChannelKinds.GetByType(channel.Type) is null)
                    throw new InvalidOperationException($"Channel type '{channel.Type}' is not supported.");
                if (!string.Equals(channel.Type, ChannelType.Internal.ToStorageString(), StringComparison.OrdinalIgnoreCase)
                    && string.IsNullOrWhiteSpace(channel.Token)
                    && (channel.Credentials is null || !channel.Credentials.Values.Any(value => !string.IsNullOrWhiteSpace(value))))
                    throw new InvalidOperationException("Channel spec.token is required.");
                break;
            case DeclarativeResourceKindItem.Integration:
                var integration = Spec<DeclarativeIntegrationSpecItem>(resource);
                if (integration.Builtin == true)
                    break;
                if (string.IsNullOrWhiteSpace(integration.Provider))
                    throw new InvalidOperationException("Integration spec.provider is required.");
                if (ResolveTransport(integration) == IntegrationTransportType.Stdio && string.IsNullOrWhiteSpace(integration.Command))
                    throw new InvalidOperationException("Stdio integrations require spec.command.");
                if (ResolveTransport(integration) != IntegrationTransportType.Stdio && string.IsNullOrWhiteSpace(integration.Url))
                    throw new InvalidOperationException("HTTP/SSE integrations require spec.url.");
                break;
            case DeclarativeResourceKindItem.MemoryStore:
                _ = Spec<DeclarativeMemoryStoreSpecItem>(resource);
                break;
            case DeclarativeResourceKindItem.Browser:
                _ = Spec<DeclarativeBrowserSpecItem>(resource);
                break;
            case DeclarativeResourceKindItem.Agent:
                var agent = Spec<DeclarativeAgentSpecItem>(resource);
                if (string.IsNullOrWhiteSpace(agent.Provider))
                    throw new InvalidOperationException("Agent spec.provider is required.");
                if (string.IsNullOrWhiteSpace(agent.Model))
                    throw new InvalidOperationException("Agent spec.model is required.");
                RequireDeclaredRefs(resource, DeclarativeResourceKindItem.Integration, agent.Integrations?.Select(item => item.Ref), resourceNames);
                RequireDeclaredRefs(resource, DeclarativeResourceKindItem.Channel, agent.Channels?.Select(item => item.Ref), resourceNames);
                RequireDeclaredRefs(resource, DeclarativeResourceKindItem.MemoryStore, agent.MemoryStores?.Select(item => item.Ref), resourceNames);
                RequireDeclaredRefs(resource, DeclarativeResourceKindItem.Browser, agent.Browsers?.Select(item => item.Ref), resourceNames);
                break;
            case DeclarativeResourceKindItem.Routine:
                var routine = Spec<DeclarativeRoutineSpecItem>(resource);
                RequireDeclaredRefs(resource, DeclarativeResourceKindItem.Agent, [routine.AgentRef], resourceNames);
                if (string.IsNullOrWhiteSpace(routine.Prompt))
                    throw new InvalidOperationException("Routine spec.prompt is required.");
                if ((routine.ScheduleTriggers?.Count ?? 0) == 0
                    && (routine.ApiTriggers?.Count ?? 0) == 0
                    && (routine.GitHubTriggers?.Count ?? 0) == 0)
                    throw new InvalidOperationException("Routine requires at least one trigger.");
                break;
        }
    }

    private async Task<DeclarativeResourceChangeItem> BuildChangeAsync(DeclarativeResourceDescriptorItem resource, Guid ownerId, Guid workspaceId, CancellationToken ct)
    {
        var existingId = resource.Kind switch
        {
            DeclarativeResourceKindItem.Integration => await ExistingIntegrationIdAsync(resource, ownerId, workspaceId, ct),
            DeclarativeResourceKindItem.Channel => await ExistingChannelIdAsync(resource, workspaceId, ct),
            DeclarativeResourceKindItem.MemoryStore => await ExistingMemoryStoreIdAsync(resource, workspaceId, ct),
            DeclarativeResourceKindItem.Browser => await ExistingBrowserIdAsync(resource, workspaceId, ct),
            DeclarativeResourceKindItem.Agent => (await FindAgentByNameAsync(resource.Name, workspaceId, ct))?.Id,
            DeclarativeResourceKindItem.Routine => await ExistingRoutineIdAsync(resource, ownerId, workspaceId, ct),
            _ => null,
        };

        return existingId.HasValue
            ? new DeclarativeResourceChangeItem(resource.Kind, resource.Name, "update", existingId.Value.ToString(), $"{resource.Kind} will be reconciled.")
            : new DeclarativeResourceChangeItem(resource.Kind, resource.Name, "create", null, $"{resource.Kind} will be created.");
    }

    private async Task<DeclarativeResourceChangeItem> ApplyIntegrationAsync(DeclarativeResourceDescriptorItem resource, Guid ownerId, Guid workspaceId, CancellationToken ct)
    {
        var spec = Spec<DeclarativeIntegrationSpecItem>(resource);
        IntegrationDefinitionRecord? integration = spec.Builtin == true
            ? IntegrationDefinitionProvider.GetBuiltin(resource.Name)
            : await _integrationDefinitionRepository.GetByNameAsync(ownerId, resource.Name, workspaceId, ct);

        var action = integration is null ? "create" : "update";
        if (spec.Builtin != true)
        {
            integration = await _integrationDefinitionService.RegisterAsync(ownerId, workspaceId, ToIntegrationDefinition(resource.Name, spec), ct);
        }

        if (spec.Credentials is { Count: > 0 })
            await _integrationDefinitionService.SaveCredentialAsync(ownerId, workspaceId, resource.Name, spec.Credentials, ct);

        integration ??= await _integrationDefinitionService.GetAsync(ownerId, resource.Name, workspaceId, ct)
            ?? IntegrationDefinitionProvider.GetBuiltin(resource.Name);

        return new DeclarativeResourceChangeItem(resource.Kind, resource.Name, action, integration?.Id.ToString(), $"{action}d integration.");
    }

    private async Task<DeclarativeResourceChangeItem> ApplyChannelAsync(DeclarativeResourceDescriptorItem resource, Guid ownerId, Guid workspaceId, CancellationToken ct)
    {
        var spec = Spec<DeclarativeChannelSpecItem>(resource);
        var id = ResourceId(workspaceId, resource.Kind, resource.Name);
        var displayName = DisplayName(resource.Name, spec.DisplayName);
        var encryptedCreds = BuildChannelCredentials(spec) is { } credentials
            ? _channelCredentialProtector.Protect(JsonSerializer.Serialize(credentials, JsonOptions))
            : null;
        var existing = await _channelRepository.GetConnectionByAsync(new ChannelConnectionFilter { Id = id, WorkspaceId = workspaceId }, ct);

        if (existing is null)
        {
            var created = await _channelRepository.CreateConnectionAsync(new ChannelConnectionRecord
            {
                Id = id,
                ChannelType = spec.Type.ToChannelType(),
                DisplayName = displayName,
                Enabled = spec.Enabled ?? true,
                CreatedById = ownerId,
                WorkspaceId = workspaceId,
                EncryptedCreds = encryptedCreds,
            }, ct);
            return new DeclarativeResourceChangeItem(resource.Kind, resource.Name, "create", created.Id.ToString(), "Created channel.");
        }

        var updated = await _channelRepository.UpdateConnectionAsync(existing.Id, channel =>
        {
            channel.DisplayName = displayName;
            channel.Enabled = spec.Enabled ?? true;
            if (encryptedCreds is not null)
                channel.EncryptedCreds = encryptedCreds;
        }, ct);
        return new DeclarativeResourceChangeItem(resource.Kind, resource.Name, "update", updated?.Id.ToString(), "Updated channel.");
    }

    private async Task<DeclarativeResourceChangeItem> ApplyMemoryStoreAsync(DeclarativeResourceDescriptorItem resource, Guid ownerId, Guid workspaceId, CancellationToken ct)
    {
        var spec = Spec<DeclarativeMemoryStoreSpecItem>(resource);
        var id = ResourceId(workspaceId, resource.Kind, resource.Name);
        var displayName = DisplayName(resource.Name, spec.DisplayName);
        var existing = await _memoryStoreRepository.GetAsync(id, null, workspaceId, ct);
        var action = existing is null ? "create" : "update";
        if (existing is null)
        {
            await _memoryStoreRepository.CreateAsync(new MemoryStoreRecord
            {
                Id = id,
                OwnerId = ownerId,
                WorkspaceId = workspaceId,
                DisplayName = displayName,
            }, ct);
        }
        else
        {
            await _memoryStoreRepository.UpdateAsync(id, null, workspaceId, displayName, ct);
        }

        foreach (var entry in spec.Entries ?? [])
            await _memoryStoreRepository.UpsertEntryAsync(id, null, workspaceId, entry.Key, entry.Content, ct);

        return new DeclarativeResourceChangeItem(resource.Kind, resource.Name, action, id.ToString(), $"{action}d memory store.");
    }

    private async Task<DeclarativeResourceChangeItem> ApplyBrowserAsync(DeclarativeResourceDescriptorItem resource, Guid ownerId, Guid workspaceId, CancellationToken ct)
    {
        var spec = Spec<DeclarativeBrowserSpecItem>(resource);
        var id = ResourceId(workspaceId, resource.Kind, resource.Name);
        var displayName = DisplayName(resource.Name, spec.DisplayName);
        var existing = await _agentResourceRepository.GetBrowserResourceAsync(id, null, workspaceId, ct);
        if (existing is null)
        {
            var created = await _agentResourceRepository.CreateBrowserResourceAsync(new BrowserResourceRecord
            {
                Id = id,
                OwnerId = ownerId,
                WorkspaceId = workspaceId,
                DisplayName = displayName,
            }, ct);
            return new DeclarativeResourceChangeItem(resource.Kind, resource.Name, "create", created.Id.ToString(), "Created browser.");
        }

        var updated = await _agentResourceRepository.UpdateBrowserResourceAsync(id, null, workspaceId, displayName, ct);
        return new DeclarativeResourceChangeItem(resource.Kind, resource.Name, "update", updated?.Id.ToString(), "Updated browser.");
    }

    private async Task<(AgentRecord Agent, DeclarativeResourceChangeItem Change)> ApplyAgentAsync(DeclarativeResourceDescriptorItem resource, Guid ownerId, Guid workspaceId, CancellationToken ct)
    {
        var spec = Spec<DeclarativeAgentSpecItem>(resource);
        var config = ToAgentDefinitionConfig(resource.Name, spec, workspaceId);
        var configJson = _agentDefinitionParser.Serialize(config);
        var existing = await FindAgentByNameAsync(resource.Name, workspaceId, ct);
        if (existing is null)
        {
            var created = await _agentDashboardService.CreateAsync(
                new CreateDashboardAgentRequest(
                    config.Name,
                    spec.Provider.Trim().ToLowerInvariant(),
                    config.Model,
                    config.System,
                    configJson,
                    spec.Integrations?.Select(item => item.Ref).ToList(),
                    spec.Channels?.Select(item => ResourceId(workspaceId, DeclarativeResourceKindItem.Channel, item.Ref)).ToList(),
                    null,
                    config.Resources?.Select(resource => new AgentResourceAttachmentRequest(resource.Type, resource.ResourceId, resource.AccessMode, resource.Instructions)).ToList(),
                    null),
                ownerId,
                workspaceId,
                ct);
            await EnsureAgentResourcesAsync(created.Id, spec, workspaceId, ct);
            return (created, new DeclarativeResourceChangeItem(resource.Kind, resource.Name, "create", created.Id.ToString(), "Created agent."));
        }

        var patched = await _agentDashboardService.PatchAsync(
            existing.Id,
            ownerId,
            workspaceId,
            new PatchAgentRequest(spec.Provider.Trim().ToLowerInvariant(), config.Model, config.Name, config.System, configJson),
            ct) ?? existing;
        await EnsureAgentResourcesAsync(patched.Id, spec, workspaceId, ct);
        return (patched, new DeclarativeResourceChangeItem(resource.Kind, resource.Name, "update", patched.Id.ToString(), "Updated agent."));
    }

    private async Task<DeclarativeResourceChangeItem> ApplyRoutineAsync(
        DeclarativeResourceDescriptorItem resource,
        IReadOnlyDictionary<string, AgentRecord> appliedAgents,
        Guid ownerId,
        Guid workspaceId,
        CancellationToken ct)
    {
        var spec = Spec<DeclarativeRoutineSpecItem>(resource);
        var agent = appliedAgents.TryGetValue(spec.AgentRef, out var applied)
            ? applied
            : await FindAgentByNameAsync(spec.AgentRef, workspaceId, ct)
                ?? throw new InvalidOperationException($"Agent '{spec.AgentRef}' was not found.");
        var existing = (await _agentRoutineService.ListForAgentAsync(agent.Id, ownerId, workspaceId, ct))
            .FirstOrDefault(routine => string.Equals(routine.Name, resource.Name, StringComparison.OrdinalIgnoreCase));
        var action = existing is null ? "create" : "update";
        if (existing is not null)
            await _agentRoutineService.DeleteAsync(existing.Id, ownerId, workspaceId, ct);

        var created = await _agentRoutineService.CreateAsync(
            new CreateAgentRoutineRequest(
                agent.Id,
                resource.Name,
                spec.Prompt,
                spec.ScheduleTriggers?.Select(trigger => new CreateScheduleRoutineTriggerRequest(trigger.Name, trigger.Expression)).ToList() ?? [],
                spec.ApiTriggers?.Select(trigger => new CreateApiRoutineTriggerRequest(trigger.Name)).ToList() ?? [],
                spec.GitHubTriggers?.Select(trigger => new CreateGitHubRoutineTriggerRequest(trigger.Name, trigger.Owner, trigger.Repo, trigger.Events ?? [], trigger.Secret)).ToList() ?? []),
            ownerId,
            workspaceId,
            ct);

        return new DeclarativeResourceChangeItem(resource.Kind, resource.Name, action, created.Routine.Id.ToString(), $"{action}d routine.");
    }

    private async Task EnsureAgentResourcesAsync(Guid agentId, DeclarativeAgentSpecItem spec, Guid workspaceId, CancellationToken ct)
    {
        var session = await _agentSessionRepository.GetByAsync(new AgentSessionFilter { AgentId = agentId, Status = SessionStatus.Active }, ct);
        if (session is null)
        {
            session = AgentSessionRecord.Create(agentId);
            await _agentSessionRepository.CreateAsync(session, ct);
        }

        var existing = await _agentResourceRepository.ListSessionAttachmentsAsync(session.Id, ct);
        foreach (var resource in ToAgentResources(spec, workspaceId))
        {
            if (!existing.Any(attachment => attachment.ResourceType == resource.Type && attachment.ResourceId == resource.ResourceId))
            {
                await _agentResourceRepository.AttachToSessionAsync(new AgentSessionResourceAttachmentRecord
                {
                    AgentId = agentId,
                    SessionId = session.Id,
                    ResourceType = resource.Type,
                    ResourceId = resource.ResourceId,
                    AccessMode = resource.AccessMode ?? AgentResourceAccessModes.ReadWrite,
                    Instructions = resource.Instructions,
                }, ct);
            }

            if (resource.Type == AgentResourceKinds.Browser)
                await _agentResourceRepository.SetBrowserCurrentAgentAsync(resource.ResourceId, agentId, ct);
            if (resource.Type == AgentResourceKinds.Channel)
            {
                var channelRef = spec.Channels?.FirstOrDefault(channel => ResourceId(workspaceId, DeclarativeResourceKindItem.Channel, channel.Ref) == resource.ResourceId);
                await _channelService.BindAgentAsync(agentId, resource.ResourceId, channelRef?.Config?.GetRawText(), ct);
            }
        }
    }

    private static AgentDefinitionConfig ToAgentDefinitionConfig(string name, DeclarativeAgentSpecItem spec, Guid workspaceId)
    {
        var integrations = (spec.Integrations ?? [])
            .Select(item => item.Ref.Trim())
            .Where(value => value.Length > 0)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();

        var tools = new List<AgentToolsetConfig>
        {
            ToToolset(AgentToolsetKinds.Builtin, null, spec.Tools?.Builtin?.PermissionPolicy),
        };

        tools.AddRange(integrations.Select(integration => ToToolset(AgentToolsetKinds.Mcp, integration, spec.Integrations?.First(item => string.Equals(item.Ref, integration, StringComparison.OrdinalIgnoreCase)).PermissionPolicy)));
        if ((spec.Browsers?.Count ?? 0) > 0 || spec.Tools?.Browser is not null)
            tools.Add(ToToolset(AgentToolsetKinds.Browser, null, spec.Tools?.Browser?.PermissionPolicy));

        return new AgentDefinitionConfig(
            name,
            string.IsNullOrWhiteSpace(spec.Description) ? null : spec.Description.Trim(),
            spec.Model?.Trim() ?? ProviderRegistry.DefaultModel,
            string.IsNullOrWhiteSpace(spec.System) ? null : spec.System.Trim(),
            integrations.Select(integration => new AgentMcpServerConfig(integration, "registered", null)).ToList(),
            tools,
            ToAgentResources(spec, workspaceId),
            null,
            spec.Metadata);
    }

    private static IReadOnlyList<AgentResourceAttachmentConfig> ToAgentResources(DeclarativeAgentSpecItem spec, Guid workspaceId)
    {
        var resources = new List<AgentResourceAttachmentConfig>();
        resources.AddRange((spec.Channels ?? []).Select(channel => new AgentResourceAttachmentConfig(
            AgentResourceKinds.Channel,
            ResourceId(workspaceId, DeclarativeResourceKindItem.Channel, channel.Ref),
            AgentResourceAccessModes.ReadWrite,
            null)));
        resources.AddRange((spec.MemoryStores ?? []).Select(memory => new AgentResourceAttachmentConfig(
            AgentResourceKinds.MemoryStore,
            ResourceId(workspaceId, DeclarativeResourceKindItem.MemoryStore, memory.Ref),
            memory.AccessMode,
            memory.Instructions)));
        resources.AddRange((spec.Browsers ?? []).Select(browser => new AgentResourceAttachmentConfig(
            AgentResourceKinds.Browser,
            ResourceId(workspaceId, DeclarativeResourceKindItem.Browser, browser.Ref),
            browser.AccessMode,
            browser.Instructions)));
        return resources;
    }

    private static AgentToolsetConfig ToToolset(string type, string? mcpServerName, DeclarativePermissionPolicyItem? policy)
        => new(type, mcpServerName, new AgentToolsetDefaultConfig(ToPermissionPolicy(policy)));

    private static AgentToolPermissionConfig ToPermissionPolicy(DeclarativePermissionPolicyItem? policy)
        => policy is null
            ? new AgentToolPermissionConfig(AgentToolPermissionKinds.AlwaysAllow, null)
            : new AgentToolPermissionConfig(policy.Type, policy.Tools);

    private static IntegrationDefinitionRecord ToIntegrationDefinition(string name, DeclarativeIntegrationSpecItem spec)
        => new()
        {
            Name = name,
            Provider = spec.Provider?.Trim() ?? "custom",
            Title = string.IsNullOrWhiteSpace(spec.Title) ? name : spec.Title.Trim(),
            Description = string.IsNullOrWhiteSpace(spec.Description) ? null : spec.Description.Trim(),
            TransportType = ResolveTransport(spec),
            Command = string.IsNullOrWhiteSpace(spec.Command) ? null : spec.Command.Trim(),
            Args = string.IsNullOrWhiteSpace(spec.Args) ? null : spec.Args.Trim(),
            Url = string.IsNullOrWhiteSpace(spec.Url) ? null : spec.Url.Trim(),
            Category = string.IsNullOrWhiteSpace(spec.Category) ? "custom" : spec.Category.Trim(),
            Logo = string.IsNullOrWhiteSpace(spec.Logo) ? null : spec.Logo,
            CredentialFieldsJson = string.IsNullOrWhiteSpace(spec.CredentialFieldsJson)
                ? BuildCredentialFieldsJson(spec.Credentials)
                : spec.CredentialFieldsJson,
        };

    private static DeclarativeAgentSpecItem FromDefinition(string provider, AgentDefinitionConfig config)
        => new(
            provider,
            config.Model,
            config.Description,
            config.System,
            new DeclarativeAgentToolsItem(
                FromToolset(config.Tools.FirstOrDefault(tool => tool.Type == AgentToolsetKinds.Builtin)),
                FromToolset(config.Tools.FirstOrDefault(tool => tool.Type == AgentToolsetKinds.Browser))),
            config.McpServers.Select(server => new DeclarativeAgentIntegrationRefItem(server.Name, null)).ToList(),
            null,
            null,
            null,
            config.Metadata);

    private static DeclarativeToolsetPolicyItem? FromToolset(AgentToolsetConfig? tool)
        => tool?.DefaultConfig?.PermissionPolicy is null
            ? null
            : new DeclarativeToolsetPolicyItem(new DeclarativePermissionPolicyItem(
                tool.DefaultConfig.PermissionPolicy.Type,
                tool.DefaultConfig.PermissionPolicy.Tools));

    private async Task<Guid?> ExistingIntegrationIdAsync(DeclarativeResourceDescriptorItem resource, Guid ownerId, Guid workspaceId, CancellationToken ct)
        => (IntegrationDefinitionProvider.GetBuiltin(resource.Name)
            ?? await _integrationDefinitionRepository.GetByNameAsync(ownerId, resource.Name, workspaceId, ct))?.Id;

    private async Task<Guid?> ExistingChannelIdAsync(DeclarativeResourceDescriptorItem resource, Guid workspaceId, CancellationToken ct)
    {
        var id = ResourceId(workspaceId, resource.Kind, resource.Name);
        return (await _channelRepository.GetConnectionByAsync(new ChannelConnectionFilter { Id = id, WorkspaceId = workspaceId }, ct))?.Id;
    }

    private async Task<Guid?> ExistingMemoryStoreIdAsync(DeclarativeResourceDescriptorItem resource, Guid workspaceId, CancellationToken ct)
    {
        var id = ResourceId(workspaceId, resource.Kind, resource.Name);
        return (await _memoryStoreRepository.GetAsync(id, null, workspaceId, ct))?.Id;
    }

    private async Task<Guid?> ExistingBrowserIdAsync(DeclarativeResourceDescriptorItem resource, Guid workspaceId, CancellationToken ct)
    {
        var id = ResourceId(workspaceId, resource.Kind, resource.Name);
        return (await _agentResourceRepository.GetBrowserResourceAsync(id, null, workspaceId, ct))?.Id;
    }

    private async Task<Guid?> ExistingRoutineIdAsync(DeclarativeResourceDescriptorItem resource, Guid ownerId, Guid workspaceId, CancellationToken ct)
    {
        var spec = Spec<DeclarativeRoutineSpecItem>(resource);
        var agent = await FindAgentByNameAsync(spec.AgentRef, workspaceId, ct);
        if (agent is null)
            return null;

        return (await _agentRoutineService.ListForAgentAsync(agent.Id, ownerId, workspaceId, ct))
            .FirstOrDefault(routine => string.Equals(routine.Name, resource.Name, StringComparison.OrdinalIgnoreCase))
            ?.Id;
    }

    private async Task<AgentRecord?> FindAgentByNameAsync(string name, Guid workspaceId, CancellationToken ct)
    {
        var agents = await _agentRepository.ListAsync(new AgentFilter { WorkspaceId = workspaceId }, ct);
        return agents.FirstOrDefault(agent => string.Equals(agent.Name, name, StringComparison.OrdinalIgnoreCase));
    }

    private static DeclarativeResourceItem NewItem<TSpec>(string kind, string name, TSpec spec)
        => new("eaos.dev/v1", kind, new DeclarativeMetadataItem(name), JsonSerializer.SerializeToElement(spec, JsonOptions));

    private static T Spec<T>(DeclarativeResourceDescriptorItem resource)
    {
        if (resource.Item.Spec is null)
            throw new InvalidOperationException($"{resource.Kind} spec is required.");

        return resource.Item.Spec.Value.Deserialize<T>(JsonOptions)
            ?? throw new InvalidOperationException($"{resource.Kind} spec is empty.");
    }

    private static IReadOnlyList<DeclarativeResourceDescriptorItem> Ordered(IReadOnlyList<DeclarativeResourceDescriptorItem> resources)
        => resources
            .OrderBy(resource => Array.IndexOf(ApplyOrder, resource.Kind))
            .ThenBy(resource => resource.Name, StringComparer.OrdinalIgnoreCase)
            .ToList();

    private static void RequireDeclaredRefs(
        DeclarativeResourceDescriptorItem resource,
        string kind,
        IEnumerable<string>? refs,
        IReadOnlyDictionary<string, HashSet<string>> resourceNames)
    {
        if (refs is null)
            return;

        resourceNames.TryGetValue(kind, out var names);
        foreach (var reference in refs.Where(reference => !string.IsNullOrWhiteSpace(reference)))
        {
            if (names is null || !names.Contains(reference.Trim()))
                throw new InvalidOperationException($"{resource.Kind} references {kind}/{reference}, but that resource is not declared in the manifest.");
        }
    }

    private static string? NormalizeKind(string? kind)
    {
        if (string.IsNullOrWhiteSpace(kind))
            return null;

        return kind.Trim().ToLowerInvariant() switch
        {
            "agent" => DeclarativeResourceKindItem.Agent,
            "channel" => DeclarativeResourceKindItem.Channel,
            "integration" => DeclarativeResourceKindItem.Integration,
            "memorystore" or "memory-store" or "memory_store" => DeclarativeResourceKindItem.MemoryStore,
            "browser" => DeclarativeResourceKindItem.Browser,
            "routine" => DeclarativeResourceKindItem.Routine,
            _ => null,
        };
    }

    private static string? NormalizeName(string? name)
        => string.IsNullOrWhiteSpace(name) ? null : name.Trim();

    private static string DisplayName(string name, string? displayName)
        => string.IsNullOrWhiteSpace(displayName) ? name : displayName.Trim();

    private static Dictionary<string, string>? BuildChannelCredentials(DeclarativeChannelSpecItem spec)
    {
        if (!string.IsNullOrWhiteSpace(spec.Token))
            return new Dictionary<string, string> { ["token"] = spec.Token.Trim() };

        return spec.Credentials is { Count: > 0 }
            ? spec.Credentials
            : null;
    }

    private static IntegrationTransportType ResolveTransport(DeclarativeIntegrationSpecItem spec)
    {
        if (!string.IsNullOrWhiteSpace(spec.TransportType)
            && Enum.TryParse<IntegrationTransportType>(spec.TransportType.Replace("-", string.Empty), true, out var parsed))
            return parsed;

        return string.IsNullOrWhiteSpace(spec.Url) ? IntegrationTransportType.Stdio : IntegrationTransportType.StreamableHttp;
    }

    private static string? BuildCredentialFieldsJson(IReadOnlyDictionary<string, string>? credentials)
    {
        if (credentials is not { Count: > 0 })
            return null;

        var fields = credentials.Keys.Select(key => new Dictionary<string, object?>
        {
            ["name"] = key,
            ["label"] = key,
            ["type"] = "password",
            ["required"] = true,
        }).ToList();
        return JsonSerializer.Serialize(fields, JsonOptions);
    }

    private static Guid ResourceId(Guid workspaceId, string kind, string name)
    {
        var hash = SHA256.HashData(Encoding.UTF8.GetBytes($"declarative:{workspaceId:N}:{kind.ToLowerInvariant()}:{name.Trim().ToLowerInvariant()}"));
        return new Guid(hash.AsSpan(0, 16));
    }
}

internal sealed record DeclarativeValidationItem(
    IReadOnlyList<DeclarativeResourceDescriptorItem> Resources,
    IReadOnlyList<DeclarativeValidationErrorItem> Errors);

internal sealed record DeclarativeResourceDescriptorItem(
    string Kind,
    string Name,
    DeclarativeResourceItem Item);

internal static class DeclarativeResourceKindItem
{
    public const string Agent = "Agent";
    public const string Channel = "Channel";
    public const string Integration = "Integration";
    public const string MemoryStore = "MemoryStore";
    public const string Browser = "Browser";
    public const string Routine = "Routine";
}

internal sealed class DeclarativeResourceTupleItem : IEqualityComparer<(string Kind, string Name)>
{
    public static readonly DeclarativeResourceTupleItem OrdinalIgnoreCase = new();

    public bool Equals((string Kind, string Name) x, (string Kind, string Name) y)
        => string.Equals(x.Kind, y.Kind, StringComparison.OrdinalIgnoreCase)
            && string.Equals(x.Name, y.Name, StringComparison.OrdinalIgnoreCase);

    public int GetHashCode((string Kind, string Name) obj)
        => HashCode.Combine(StringComparer.OrdinalIgnoreCase.GetHashCode(obj.Kind), StringComparer.OrdinalIgnoreCase.GetHashCode(obj.Name));
}
