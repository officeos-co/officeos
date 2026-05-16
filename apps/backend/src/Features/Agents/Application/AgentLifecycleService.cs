namespace OffceOs.Application.Features.Agents;

internal sealed class AgentLifecycleService : IAgentLifecycleService
{
    private readonly IAgentService _agentService;
    private readonly IAgentRepository _agentRepository;
    private readonly IAgentSessionRepository _agentSessionRepository;
    private readonly IAgentResourceRepository _agentResourceRepository;
    private readonly IBrowserResourceRepository _browserResourceRepository;
    private readonly IMemoryStoreRepository _memoryStoreRepository;
    private readonly IChannelRepository _channelRepository;
    private readonly IChannelService _channelService;
    private readonly IBrowserService _browserService;
    private readonly IAgentDeployer _agentDeployer;
    private readonly IAgentLogService _agentLogService;
    private readonly IAgentDefinitionRepository _agentDefinitionRepository;
    private readonly AgentDefinitionParser _agentDefinitionParser;
    private readonly IAgentRoutineService _agentRoutineService;

    public AgentLifecycleService(
        IAgentService agents,
        IAgentRepository agentRepository,
        IAgentSessionRepository sessions,
        IAgentResourceRepository resources,
        IBrowserResourceRepository browserResourceRepository,
        IMemoryStoreRepository memoryStores,
        IChannelRepository channelRepository,
        IChannelService channelService,
        IBrowserService browser,
        IAgentDeployer agentDeployer,
        IAgentLogService agentLogService,
        IAgentDefinitionRepository agentDefinitionRepository,
        AgentDefinitionParser agentDefinitionParser,
        IAgentRoutineService agentRoutineService)
    {
        _agentService = agents;
        _agentRepository = agentRepository;
        _agentSessionRepository = sessions;
        _agentResourceRepository = resources;
        _browserResourceRepository = browserResourceRepository;
        _memoryStoreRepository = memoryStores;
        _channelRepository = channelRepository;
        _channelService = channelService;
        _browserService = browser;
        _agentDeployer = agentDeployer;
        _agentLogService = agentLogService;
        _agentDefinitionRepository = agentDefinitionRepository;
        _agentDefinitionParser = agentDefinitionParser;
        _agentRoutineService = agentRoutineService;
    }

    public async Task<IReadOnlyList<AgentLifecycleResult>> ListAgentsAsync(
        Guid ownerId,
        Guid workspaceId,
        CancellationToken ct = default)
    {
        var agents = await _agentRepository.ListAsync(new AgentFilter { WorkspaceId = workspaceId }, ct);
        var lastMessages = await _agentLogService.GetLastRelevantMessagesAsync(
            new LastRelevantLogQueryRequest(
                AgentIds: agents.Select(agent => agent.Id).ToList(),
                WorkspaceId: workspaceId),
            ct);

        var results = new List<AgentLifecycleResult>(agents.Count);
        foreach (var agent in agents)
        {
            var status = await ResolveStatusAsync(agent, ct);
            results.Add(new AgentLifecycleResult(
                agent,
                status,
                lastMessages.TryGetValue(agent.Id, out var lastMessage) ? lastMessage : null));
        }

        return results;
    }

    public async Task<AgentLifecycleResult?> GetAgentAsync(
        Guid id,
        Guid ownerId,
        Guid workspaceId,
        CancellationToken ct = default)
    {
        var agent = await _agentRepository.GetByAsync(new AgentFilter { Id = id, WorkspaceId = workspaceId }, ct);
        if (agent is null)
            return null;

        var status = await ResolveStatusAsync(agent, ct);
        var lastMessages = await _agentLogService.GetLastRelevantMessagesAsync(
            new LastRelevantLogQueryRequest(AgentIds: [agent.Id], WorkspaceId: workspaceId),
            ct);
        var lastMessage = lastMessages.TryGetValue(agent.Id, out var value) ? value : null;
        return new AgentLifecycleResult(agent, status, lastMessage);
    }

    public async Task<AgentRecord> CreateAsync(CreateAgentLifecycleRequest request, Guid ownerId, Guid workspaceId, CancellationToken ct = default)
    {
        await EnsureChannelConnectionsExistAsync(request.ChannelConnectionIds, workspaceId, ct);
        var definitionConfig = string.IsNullOrWhiteSpace(request.ConfigJson)
            ? null
            : _agentDefinitionParser.Parse(request.ConfigJson);

        var agent = await _agentService.CreateAsync(
            new CreateAgentRequest(
                request.Name,
                request.Provider,
                request.Model,
                request.Prompt,
                definitionConfig is null
                    ? _agentDefinitionParser.Serialize(_agentDefinitionParser.CreateDefaultConfig(
                    request.Name,
                    string.IsNullOrWhiteSpace(request.Model) ? ProviderRegistry.DefaultModel : request.Model,
                    request.Prompt,
                    request.ToolNames is { Count: > 0 } ? request.ToolNames : request.IntegrationSlugs))
                    : _agentDefinitionParser.Serialize(definitionConfig)),
            ownerId,
            workspaceId,
            ct);

        var resources = MergeResources(request.Resources, definitionConfig?.Resources);
        if (resources.Count > 0)
        {
            var resourceSession = await _agentSessionRepository.GetByAsync(
                new AgentSessionFilter { AgentId = agent.Id, Status = SessionStatus.Active },
                ct);
            if (resourceSession is null)
            {
                resourceSession = AgentSessionRecord.Create(agent.Id);
                await _agentSessionRepository.CreateAsync(resourceSession, ct);
            }

            foreach (var resource in resources)
            {
                var resourceType = NormalizeResourceType(resource.ResourceType);
                await EnsureResourceExistsAsync(resourceType, resource.ResourceId, ownerId, workspaceId, ct);

                await _agentResourceRepository.AttachToSessionAsync(new AgentSessionResourceAttachmentRecord
                {
                    AgentId = agent.Id,
                    SessionId = resourceSession.Id,
                    ResourceType = resourceType,
                    ResourceId = resource.ResourceId,
                    AccessMode = NormalizeAccessMode(resource.AccessMode),
                    Instructions = string.IsNullOrWhiteSpace(resource.Instructions) ? null : resource.Instructions.Trim(),
                }, ct);

                if (resourceType == AgentResourceKinds.Browser)
                    await _browserResourceRepository.SetCurrentAgentAsync(resource.ResourceId, agent.Id, ct);
                if (resourceType == AgentResourceKinds.Channel)
                    await _channelService.BindAgentAsync(agent.Id, resource.ResourceId, null, ct);
            }
        }

        var toolNames = request.ToolNames is { Count: > 0 }
            ? request.ToolNames
            : request.IntegrationSlugs;
        var bootstrap = !string.IsNullOrWhiteSpace(request.BootstrapMessage)
            ? request.BootstrapMessage
            : AgentPersonalityRecord.CreateBootstrapContent(agent.Prompt);

        await _agentService.InitializeAgentAsync(
            agent.Id,
            ownerId,
            new AgentInitRequest(toolNames, request.ChannelConnectionIds, bootstrap),
            ct);

        if (definitionConfig?.Routines is { Count: > 0 })
        {
            foreach (var routine in definitionConfig.Routines)
            {
                await _agentRoutineService.CreateAsync(
                    new CreateAgentRoutineRequest(
                        agent.Id,
                        routine.Name,
                        routine.Prompt,
                        routine.ScheduleTriggers?.Select(trigger => new CreateScheduleRoutineTriggerRequest(trigger.Name, trigger.Expression)).ToList() ?? [],
                        routine.ApiTriggers?.Select(trigger => new CreateApiRoutineTriggerRequest(trigger.Name)).ToList() ?? [],
                        routine.GitHubTriggers?.Select(trigger => new CreateGitHubRoutineTriggerRequest(
                            trigger.Name,
                            trigger.Repo,
                            trigger.Events,
                            trigger.AuthRef,
                            trigger.Secret,
                            trigger.Mode,
                            trigger.PollIntervalSeconds)).ToList() ?? []),
                    ownerId,
                    workspaceId,
                    ct);
            }
        }

        return agent;
    }

    public async Task<AgentRecord?> PatchAsync(Guid id, Guid ownerId, Guid workspaceId, PatchAgentRequest request, CancellationToken ct = default)
    {
        if (!await AgentIsOwnedAsync(id, ownerId, workspaceId, ct))
            return null;

        return await _agentService.PatchAsync(id, request, ct);
    }

    public async Task<bool> RebootstrapAsync(Guid id, Guid ownerId, Guid workspaceId, CancellationToken ct = default)
    {
        if (!await AgentIsOwnedAsync(id, ownerId, workspaceId, ct))
            return false;

        var activeDefinition = await _agentDefinitionRepository.GetByAsync(
            new AgentDefinitionFilter { AgentId = id, ActiveOnly = true },
            ct);
        if (activeDefinition is null)
            return false;

        var config = _agentDefinitionParser.Parse(activeDefinition.ConfigJson);
        if (string.IsNullOrWhiteSpace(config.System))
            return false;

        await _agentService.InitializeAgentAsync(
            id,
            ownerId,
            new AgentInitRequest(
                config.McpServers.Select(server => server.Name).ToList(),
                null,
                AgentPersonalityRecord.CreateBootstrapContent(config.System)),
            ct);
        return true;
    }

    public async Task<bool> DeleteAsync(Guid id, Guid ownerId, Guid workspaceId, CancellationToken ct = default)
    {
        if (!await AgentIsOwnedAsync(id, ownerId, workspaceId, ct))
            return false;

        await _browserService.StopAsync(id, ct);
        return await _agentService.DeleteAsync(id, ct);
    }

    private async Task<bool> AgentIsOwnedAsync(Guid agentId, Guid ownerId, Guid workspaceId, CancellationToken ct)
    {
        var agent = await _agentRepository.GetByAsync(new AgentFilter { Id = agentId, WorkspaceId = workspaceId }, ct);
        return agent is not null;
    }

    private async Task EnsureAgentOwnedAsync(Guid agentId, Guid ownerId, Guid workspaceId, CancellationToken ct)
    {
        if (!await AgentIsOwnedAsync(agentId, ownerId, workspaceId, ct))
            throw new InvalidOperationException("Agent not found.");
    }

    private async Task<AgentStatus> ResolveStatusAsync(AgentRecord agent, CancellationToken ct)
    {
        if (!agent.HasPod || string.IsNullOrWhiteSpace(agent.PodName))
            return agent.Status == AgentStatus.Failed ? AgentStatus.Failed : AgentStatus.Booting;

        var runtimeStatus = await GetRuntimeStatusAsync(agent, ct);
        if (runtimeStatus is AgentStatus.Failed or AgentStatus.Booting or AgentStatus.Restarting)
            return runtimeStatus;

        var runningWork = await _agentLogService.ListAsync(new AgentLogQueryRequest(
            WorkspaceId: agent.WorkspaceId,
            AgentId: agent.Id,
            Type: AgentLogType.MessageIn,
            WorkStatus: AgentWorkStatusKinds.Running,
            Limit: 1), ct);

        return runningWork.Items.Count > 0
            ? AgentStatus.Working
            : AgentStatus.Idle;
    }

    private async Task<AgentStatus> GetRuntimeStatusAsync(AgentRecord agent, CancellationToken ct)
    {
        try
        {
            var status = (await _agentDeployer.GetStatusAsync(agent.PodName!, ct)).ToAgentStatus();
            var persistedStatus = status == AgentStatus.Working ? AgentStatus.Idle : status;
            if (persistedStatus != agent.Status)
            {
                await _agentRepository.UpdateStatusAsync(new AgentFilter { Id = agent.Id }, persistedStatus, ct);
                agent.Status = persistedStatus;
            }

            return status;
        }
        catch
        {
            return agent.Status == AgentStatus.Failed ? AgentStatus.Failed : AgentStatus.Booting;
        }
    }

    private async Task EnsureResourceExistsAsync(string resourceType, Guid resourceId, Guid ownerId, Guid workspaceId, CancellationToken ct)
    {
        var exists = resourceType switch
        {
            AgentResourceKinds.Browser => await _browserResourceRepository.GetByAsync(new BrowserResourceFilter { Id = resourceId, WorkspaceId = workspaceId }, ct) is not null,
            AgentResourceKinds.MemoryStore => await _memoryStoreRepository.GetAsync(resourceId, null, workspaceId, ct) is not null,
            AgentResourceKinds.Channel => await _channelRepository.GetConnectionByAsync(new ChannelConnectionFilter
            {
                Id = resourceId,
                WorkspaceId = workspaceId,
            }, ct) is not null,
            _ => false,
        };

        if (!exists)
            throw new InvalidOperationException("Resource not found.");
    }

    private async Task EnsureChannelConnectionsExistAsync(
        IReadOnlyList<Guid>? channelConnectionIds,
        Guid workspaceId,
        CancellationToken ct)
    {
        if (channelConnectionIds is not { Count: > 0 })
            return;

        foreach (var channelConnectionId in channelConnectionIds.Distinct())
        {
            var connection = await _channelRepository.GetConnectionByAsync(new ChannelConnectionFilter
            {
                Id = channelConnectionId,
                WorkspaceId = workspaceId,
            }, ct);

            if (connection is null)
                throw new InvalidOperationException("Channel connection not found.");
        }
    }

    private static IReadOnlyList<AgentResourceAttachmentRequest> MergeResources(
        IReadOnlyList<AgentResourceAttachmentRequest>? requestResources,
        IReadOnlyList<AgentResourceAttachmentConfig>? configResources)
    {
        var resources = new List<AgentResourceAttachmentRequest>();
        if (requestResources is { Count: > 0 })
            resources.AddRange(requestResources);
        if (configResources is { Count: > 0 })
        {
            resources.AddRange(configResources.Select(resource => new AgentResourceAttachmentRequest(
                resource.Type,
                resource.ResourceId,
                resource.AccessMode,
                resource.Instructions)));
        }

        return resources
            .GroupBy(resource => (Type: NormalizeResourceType(resource.ResourceType), resource.ResourceId))
            .Select(group => group.Last())
            .ToList();
    }

    private static string NormalizeResourceType(string resourceType)
    {
        var normalized = resourceType.Trim().ToLowerInvariant();
        return normalized is AgentResourceKinds.Browser or AgentResourceKinds.MemoryStore or AgentResourceKinds.Channel
            ? normalized
            : throw new InvalidOperationException($"Unsupported resource type '{resourceType}'.");
    }

    private static string NormalizeAccessMode(string? accessMode)
    {
        var normalized = string.IsNullOrWhiteSpace(accessMode)
            ? AgentResourceAccessModes.ReadWrite
            : accessMode.Trim().ToLowerInvariant();
        return normalized is AgentResourceAccessModes.ReadWrite or AgentResourceAccessModes.ReadOnly
            ? normalized
            : AgentResourceAccessModes.ReadWrite;
    }
}
