namespace EnterpriseAgentOs.Application.Features.Agents;

internal sealed class AgentDashboardService : IAgentDashboardService
{
    private readonly IAgentService _agents;
    private readonly IAgentRepository _agentRepository;
    private readonly IAgentSessionRepository _sessions;
    private readonly IAgentResourceRepository _resources;
    private readonly IMemoryStoreRepository _memoryStores;
    private readonly IChannelRepository _channelRepository;
    private readonly IChannelService _channelService;
    private readonly IBrowserService _browser;
    private readonly IAgentToolPermissionRepository _permissions;
    private readonly IAgentRunRepository _runs;

    public AgentDashboardService(
        IAgentService agents,
        IAgentRepository agentRepository,
        IAgentSessionRepository sessions,
        IAgentResourceRepository resources,
        IMemoryStoreRepository memoryStores,
        IChannelRepository channelRepository,
        IChannelService channelService,
        IBrowserService browser,
        IAgentToolPermissionRepository permissions,
        IAgentRunRepository runs)
    {
        _agents = agents;
        _agentRepository = agentRepository;
        _sessions = sessions;
        _resources = resources;
        _memoryStores = memoryStores;
        _channelRepository = channelRepository;
        _channelService = channelService;
        _browser = browser;
        _permissions = permissions;
        _runs = runs;
    }

    public async Task<AgentRecord> CreateAsync(CreateDashboardAgentRequest request, Guid ownerId, CancellationToken ct = default)
    {
        var agent = await _agents.CreateAsync(
            new CreateAgentRequest(request.Name, request.Provider, request.Model, request.Prompt),
            ownerId,
            ct);

        if (request.Resources is { Count: > 0 })
        {
            var resourceSession = await _sessions.GetByAsync(
                new AgentSessionFilter { AgentId = agent.Id, Status = SessionStatus.Active },
                ct);
            if (resourceSession is null)
            {
                resourceSession = AgentSessionRecord.Create(agent.Id);
                await _sessions.CreateAsync(resourceSession, ct);
            }

            foreach (var resource in request.Resources)
            {
                var resourceType = NormalizeResourceType(resource.ResourceType);
                await EnsureResourceExistsAsync(resourceType, resource.ResourceId, ownerId, ct);

                await _resources.AttachToSessionAsync(new AgentSessionResourceAttachmentRecord
                {
                    AgentId = agent.Id,
                    SessionId = resourceSession.Id,
                    ResourceType = resourceType,
                    ResourceId = resource.ResourceId,
                    AccessMode = NormalizeAccessMode(resource.AccessMode),
                    Instructions = string.IsNullOrWhiteSpace(resource.Instructions) ? null : resource.Instructions.Trim(),
                }, ct);

                if (resourceType == AgentResourceKinds.Browser)
                    await _resources.SetBrowserCurrentAgentAsync(resource.ResourceId, agent.Id, ct);
                if (resourceType == AgentResourceKinds.Channel)
                    await _channelService.BindAgentAsync(agent.Id, resource.ResourceId, null, ct);
            }
        }

        var toolNames = request.ToolNames is { Count: > 0 }
            ? request.ToolNames
            : request.IntegrationSlugs;

        var bootstrap = !string.IsNullOrWhiteSpace(request.BootstrapMessage)
            ? request.BootstrapMessage
            : request.Prompt;

        await _agents.InitializeAgentAsync(
            agent.Id,
            ownerId,
            new AgentInitRequest(toolNames, request.ToolPermissions, request.ChannelSlugs, bootstrap),
            ct);

        return agent;
    }

    public async Task<AgentRecord?> PatchAsync(Guid id, Guid ownerId, PatchAgentRequest request, CancellationToken ct = default)
    {
        if (!await AgentIsOwnedAsync(id, ownerId, ct))
            return null;

        return await _agents.PatchAsync(id, request, ct);
    }

    public async Task<bool> DeleteAsync(Guid id, Guid ownerId, CancellationToken ct = default)
    {
        if (!await AgentIsOwnedAsync(id, ownerId, ct))
            return false;

        await _browser.StopAsync(id, ct);
        return await _agents.DeleteAsync(id, ct);
    }

    public async Task<IReadOnlyList<AgentToolPermissionRecord>> ListToolPermissionsAsync(
        Guid ownerId,
        Guid agentId,
        CancellationToken ct = default)
    {
        await EnsureAgentOwnedAsync(agentId, ownerId, ct);
        return await _permissions.ListForAgentAsync(agentId, ct);
    }

    public async Task<IReadOnlyList<AgentRunRecord>> ListRunsAsync(
        Guid ownerId,
        Guid agentId,
        Guid? parentRunId,
        CancellationToken ct = default)
    {
        await EnsureAgentOwnedAsync(agentId, ownerId, ct);
        return await _runs.ListForAgentAsync(agentId, parentRunId, ct);
    }

    public async Task SetToolPermissionAsync(Guid ownerId, Guid agentId, string skill, string tool, ToolPermission mode, CancellationToken ct = default)
    {
        await EnsureAgentOwnedAsync(agentId, ownerId, ct);
        await _permissions.UpsertAsync(agentId, skill, tool, mode, ct);
    }

    public async Task<IReadOnlyList<AgentToolPermissionRecord>> SetToolPermissionsAsync(
        Guid ownerId,
        Guid agentId,
        IReadOnlyList<AgentToolPermissionRecord> rows,
        CancellationToken ct = default)
    {
        await EnsureAgentOwnedAsync(agentId, ownerId, ct);
        await _permissions.SetManyAsync(agentId, rows, ct);
        return rows;
    }

    private async Task<bool> AgentIsOwnedAsync(Guid agentId, Guid ownerId, CancellationToken ct)
    {
        var agent = await _agentRepository.GetByAsync(new AgentFilter { Id = agentId, OwnerId = ownerId }, ct);
        return agent is not null;
    }

    private async Task EnsureAgentOwnedAsync(Guid agentId, Guid ownerId, CancellationToken ct)
    {
        if (!await AgentIsOwnedAsync(agentId, ownerId, ct))
            throw new InvalidOperationException("Agent not found.");
    }

    private async Task EnsureResourceExistsAsync(string resourceType, Guid resourceId, Guid ownerId, CancellationToken ct)
    {
        var exists = resourceType switch
        {
            AgentResourceKinds.Browser => await _resources.GetBrowserResourceAsync(resourceId, ownerId, ct) is not null,
            AgentResourceKinds.MemoryStore => await _memoryStores.GetAsync(resourceId, ownerId, ct) is not null,
            AgentResourceKinds.Channel => await _channelRepository.GetConnectionByAsync(new ChannelConnectionFilter
            {
                Id = resourceId,
                CreatedById = ownerId,
            }, ct) is not null,
            _ => false,
        };

        if (!exists)
            throw new InvalidOperationException("Resource not found.");
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
