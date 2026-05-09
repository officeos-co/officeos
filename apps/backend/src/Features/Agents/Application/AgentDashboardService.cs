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

    public async Task<AgentDto> CreateAsync(CreateDashboardAgentRequest request, Guid ownerId, CancellationToken ct = default)
    {
        var dto = await _agents.CreateAsync(
            new CreateAgentRequest(request.Name, request.Provider, request.Model, request.Prompt),
            ownerId,
            ct);

        if (request.Resources is { Count: > 0 })
        {
            var resourceSession = await _sessions.GetByAsync(
                new AgentSessionFilter { AgentId = dto.Id, Status = SessionStatus.Active },
                ct);
            if (resourceSession is null)
            {
                resourceSession = AgentSessionRecord.Create(dto.Id);
                await _sessions.CreateAsync(resourceSession, ct);
            }

            foreach (var resource in request.Resources)
            {
                var resourceType = NormalizeResourceType(resource.ResourceType);
                await EnsureResourceExistsAsync(resourceType, resource.ResourceId, ownerId, ct);

                await _resources.AttachToSessionAsync(new AgentSessionResourceAttachmentRecord
                {
                    AgentId = dto.Id,
                    SessionId = resourceSession.Id,
                    ResourceType = resourceType,
                    ResourceId = resource.ResourceId,
                    AccessMode = NormalizeAccessMode(resource.AccessMode),
                    Instructions = string.IsNullOrWhiteSpace(resource.Instructions) ? null : resource.Instructions.Trim(),
                }, ct);

                if (resourceType == AgentResourceTypes.Browser)
                    await _resources.SetBrowserCurrentAgentAsync(resource.ResourceId, dto.Id, ct);
                if (resourceType == AgentResourceTypes.Channel)
                    await _channelService.BindAgentAsync(dto.Id, resource.ResourceId, null, ct);
            }
        }

        var toolNames = request.ToolNames is { Count: > 0 }
            ? request.ToolNames
            : request.IntegrationSlugs;

        var bootstrap = !string.IsNullOrWhiteSpace(request.BootstrapMessage)
            ? request.BootstrapMessage
            : request.Prompt;

        await _agents.InitializeAgentAsync(
            dto.Id,
            ownerId,
            new AgentInitRequest(toolNames, request.ToolPermissions, request.ChannelSlugs, bootstrap),
            ct);

        return dto;
    }

    public async Task<AgentDto?> PatchAsync(Guid id, Guid ownerId, PatchAgentRequest request, CancellationToken ct = default)
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
            AgentResourceTypes.Browser => await _resources.GetBrowserResourceAsync(resourceId, ownerId, ct) is not null,
            AgentResourceTypes.MemoryStore => await _memoryStores.GetAsync(resourceId, ownerId, ct) is not null,
            AgentResourceTypes.Channel => await _channelRepository.GetConnectionByAsync(new ChannelConnectionFilter
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
        return normalized is AgentResourceTypes.Browser or AgentResourceTypes.MemoryStore or AgentResourceTypes.Channel
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
