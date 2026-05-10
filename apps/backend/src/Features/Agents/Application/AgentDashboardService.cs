namespace OffceOs.Application.Features.Agents;

internal sealed class AgentDashboardService : IAgentDashboardService
{
    private readonly IAgentService _agentService;
    private readonly IAgentRepository _agentRepository;
    private readonly IAgentSessionRepository _agentSessionRepository;
    private readonly IAgentResourceRepository _agentResourceRepository;
    private readonly IMemoryStoreRepository _memoryStoreRepository;
    private readonly IChannelRepository _channelRepository;
    private readonly IChannelService _channelService;
    private readonly IBrowserService _browserService;
    private readonly IAgentToolPermissionRepository _agentToolPermissionRepository;
    private readonly IAgentRunRepository _agentRunRepository;

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
        _agentService = agents;
        _agentRepository = agentRepository;
        _agentSessionRepository = sessions;
        _agentResourceRepository = resources;
        _memoryStoreRepository = memoryStores;
        _channelRepository = channelRepository;
        _channelService = channelService;
        _browserService = browser;
        _agentToolPermissionRepository = permissions;
        _agentRunRepository = runs;
    }

    public async Task<AgentRecord> CreateAsync(CreateDashboardAgentRequest request, Guid ownerId, Guid workspaceId, CancellationToken ct = default)
    {
        await EnsureChannelConnectionsExistAsync(request.ChannelConnectionIds, workspaceId, ct);

        var agent = await _agentService.CreateAsync(
            new CreateAgentRequest(request.Name, request.Provider, request.Model, request.Prompt),
            ownerId,
            workspaceId,
            ct);

        if (request.Resources is { Count: > 0 })
        {
            var resourceSession = await _agentSessionRepository.GetByAsync(
                new AgentSessionFilter { AgentId = agent.Id, Status = SessionStatus.Active },
                ct);
            if (resourceSession is null)
            {
                resourceSession = AgentSessionRecord.Create(agent.Id);
                await _agentSessionRepository.CreateAsync(resourceSession, ct);
            }

            foreach (var resource in request.Resources)
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
                    await _agentResourceRepository.SetBrowserCurrentAgentAsync(resource.ResourceId, agent.Id, ct);
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

        await _agentService.InitializeAgentAsync(
            agent.Id,
            ownerId,
            new AgentInitRequest(toolNames, request.ToolPermissions, request.ChannelConnectionIds, bootstrap),
            ct);

        return agent;
    }

    public async Task<AgentRecord?> PatchAsync(Guid id, Guid ownerId, Guid workspaceId, PatchAgentRequest request, CancellationToken ct = default)
    {
        if (!await AgentIsOwnedAsync(id, ownerId, workspaceId, ct))
            return null;

        return await _agentService.PatchAsync(id, request, ct);
    }

    public async Task<bool> DeleteAsync(Guid id, Guid ownerId, Guid workspaceId, CancellationToken ct = default)
    {
        if (!await AgentIsOwnedAsync(id, ownerId, workspaceId, ct))
            return false;

        await _browserService.StopAsync(id, ct);
        return await _agentService.DeleteAsync(id, ct);
    }

    public async Task<IReadOnlyList<AgentToolPermissionRecord>> ListToolPermissionsAsync(
        Guid ownerId,
        Guid workspaceId,
        Guid agentId,
        CancellationToken ct = default)
    {
        await EnsureAgentOwnedAsync(agentId, ownerId, workspaceId, ct);
        return await _agentToolPermissionRepository.ListForAgentAsync(agentId, ct);
    }

    public async Task<IReadOnlyList<AgentRunRecord>> ListRunsAsync(
        Guid ownerId,
        Guid workspaceId,
        Guid agentId,
        Guid? parentRunId,
        CancellationToken ct = default)
    {
        await EnsureAgentOwnedAsync(agentId, ownerId, workspaceId, ct);
        return await _agentRunRepository.ListForAgentAsync(agentId, parentRunId, ct);
    }

    public async Task SetToolPermissionAsync(Guid ownerId, Guid workspaceId, Guid agentId, string skill, string tool, ToolPermission mode, CancellationToken ct = default)
    {
        await EnsureAgentOwnedAsync(agentId, ownerId, workspaceId, ct);
        await _agentToolPermissionRepository.UpsertAsync(agentId, skill, tool, mode, ct);
    }

    public async Task<IReadOnlyList<AgentToolPermissionRecord>> SetToolPermissionsAsync(
        Guid ownerId,
        Guid workspaceId,
        Guid agentId,
        IReadOnlyList<AgentToolPermissionRecord> rows,
        CancellationToken ct = default)
    {
        await EnsureAgentOwnedAsync(agentId, ownerId, workspaceId, ct);
        await _agentToolPermissionRepository.SetManyAsync(agentId, rows, ct);
        return rows;
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

    private async Task EnsureResourceExistsAsync(string resourceType, Guid resourceId, Guid ownerId, Guid workspaceId, CancellationToken ct)
    {
        var exists = resourceType switch
        {
            AgentResourceKinds.Browser => await _agentResourceRepository.GetBrowserResourceAsync(resourceId, null, workspaceId, ct) is not null,
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
