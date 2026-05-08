namespace EnterpriseAgentOs.Api.Features.Agents;

[ExtendObjectType(typeof(GraphQLMutations))]
public class AgentDashboardMutations
{
    private static string AgentListQueryCacheKey(Guid userId) => $"agents:dashboard:list:{userId}";
    private static string AgentQueryCacheKey(Guid id, Guid userId) => $"agents:dashboard:{id}:user:{userId}";

    [GraphQLDescription("Creates a new agent with the given config. Optionally assigns skills, tool permissions, and channels.")]
    public async Task<AgentDto> CreateAgent(
        CreateAgentInput input,
        IResolverContext context,
        [Service] IAgentService agents,
        [Service] IAgentSessionRepository sessions,
        [Service] IAgentResourceRepository resources,
        [Service] IMemoryStoreRepository memoryStores,
        [Service] IChannelRepository channelRepository,
        [Service] IChannelService channelService,
        [Service] IDistributedCache cache,
        CancellationToken ct)
    {
        var user = DashboardAuthContextExtensions.GetUser(context);
        AgentDto dto;
        try
        {
            dto = await agents.CreateAsync(
                new CreateAgentRequest(input.Name, input.Provider, input.Model, input.Prompt),
                ownerId: user.Id,
                ct);
        }
        catch (InvalidOperationException ex)
        {
            throw new GraphQLException(
                ErrorBuilder.New()
                    .SetMessage(ex.Message)
                    .SetCode("VALIDATION")
                    .Build());
        }

        var toolNames = input.ToolNames is { Count: > 0 }
            ? input.ToolNames
            : input.IntegrationSlugs;

        var bootstrap = !string.IsNullOrWhiteSpace(input.BootstrapMessage)
            ? input.BootstrapMessage
            : input.Prompt;

        AgentSessionRecord? resourceSession = null;
        if (input.Resources is { Count: > 0 })
        {
            resourceSession = await sessions.GetByAsync(
                new AgentSessionFilter { AgentId = dto.Id, Status = SessionStatus.Active },
                ct);
            if (resourceSession is null)
            {
                resourceSession = AgentSessionRecord.Create(dto.Id);
                await sessions.CreateAsync(resourceSession, ct);
            }

            foreach (var resource in input.Resources)
            {
                var resourceType = NormalizeResourceType(resource.ResourceType);
                await ValidateResourceAsync(resourceType, resource.ResourceId, user.Id, resources, memoryStores, channelRepository, ct);

                await resources.AttachToSessionAsync(new AgentSessionResourceAttachmentRecord
                {
                    AgentId = dto.Id,
                    SessionId = resourceSession.Id,
                    ResourceType = resourceType,
                    ResourceId = resource.ResourceId,
                    AccessMode = NormalizeAccessMode(resource.AccessMode),
                    Instructions = string.IsNullOrWhiteSpace(resource.Instructions) ? null : resource.Instructions.Trim(),
                }, ct);

                if (resourceType == AgentResourceTypes.Browser)
                    await resources.SetBrowserCurrentAgentAsync(resource.ResourceId, dto.Id, ct);
                if (resourceType == AgentResourceTypes.Channel)
                    await channelService.BindAgentAsync(dto.Id, resource.ResourceId, null, ct);
            }
        }

        await agents.InitializeAgentAsync(
            dto.Id,
            user.Id,
            new AgentInitRequest(
                toolNames,
                input.ToolPermissions?.Select(tp => new AgentToolPermissionInit(tp.Tool, tp.Mode)).ToList(),
                input.ChannelSlugs,
                bootstrap),
            ct);

        await cache.RemoveAsync(AgentListQueryCacheKey(user.Id), ct);
        return dto;
    }

    private static string NormalizeResourceType(string resourceType)
    {
        var normalized = resourceType.Trim().ToLowerInvariant();
        if (normalized is AgentResourceTypes.Browser or AgentResourceTypes.MemoryStore or AgentResourceTypes.Channel)
            return normalized;
        throw new GraphQLException(
            ErrorBuilder.New()
                .SetMessage($"Unsupported resource type '{resourceType}'.")
                .SetCode("VALIDATION")
                .Build());
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

    private static async Task ValidateResourceAsync(
        string resourceType,
        Guid resourceId,
        Guid ownerId,
        IAgentResourceRepository resources,
        IMemoryStoreRepository memoryStores,
        IChannelRepository channelRepository,
        CancellationToken ct)
    {
        var exists = resourceType switch
        {
            AgentResourceTypes.Browser => await resources.GetBrowserResourceAsync(resourceId, ownerId, ct) is not null,
            AgentResourceTypes.MemoryStore => await memoryStores.GetAsync(resourceId, ownerId, ct) is not null,
            AgentResourceTypes.Channel => await channelRepository.GetConnectionByAsync(new ChannelConnectionFilter
            {
                Id = resourceId,
                CreatedById = ownerId,
            }, ct) is not null,
            _ => false,
        };
        if (!exists)
        {
            throw new GraphQLException(
                ErrorBuilder.New()
                    .SetMessage("Resource not found.")
                    .SetCode("NOT_FOUND")
                    .Build());
        }
    }

    [GraphQLDescription("Patches mutable fields on an existing agent (name, provider, model, prompt). Null fields are left unchanged.")]
    public async Task<AgentDto> UpdateAgent(
        Guid id,
        UpdateAgentInput input,
        IResolverContext context,
        [Service] IAgentService agents,
        [Service] IDistributedCache cache,
        CancellationToken ct)
    {
        var user = DashboardAuthContextExtensions.GetUser(context);
        var dto = await agents.PatchAsync(
            id,
            new PatchAgentRequest(input.Provider, input.Model, input.Name, input.Prompt),
            ct);
        if (dto is null)
        {
            throw new GraphQLException(
                ErrorBuilder.New()
                    .SetMessage($"Agent '{id}' not found.")
                    .SetCode("NOT_FOUND")
                    .Build());
        }
        await cache.RemoveAsync(AgentListQueryCacheKey(user.Id), ct);
        await cache.RemoveAsync(AgentQueryCacheKey(id, user.Id), ct);
        return dto;
    }

    [GraphQLDescription("Soft-deletes an agent and removes its Kubernetes pod.")]
    public async Task<bool> DeleteAgent(
        Guid id,
        IResolverContext context,
        [Service] IAgentService agents,
        [Service] IBrowserService browser,
        [Service] IDistributedCache cache,
        CancellationToken ct)
    {
        var user = DashboardAuthContextExtensions.GetUser(context);
        await browser.StopAsync(id, ct);
        var result = await agents.DeleteAsync(id, ct);
        await cache.RemoveAsync(AgentListQueryCacheKey(user.Id), ct);
        await cache.RemoveAsync(AgentQueryCacheKey(id, user.Id), ct);
        return result;
    }

    [GraphQLDescription("Sets one explicit tool permission override for an agent.")]
    public async Task<ToolPermissionPayload> SetAgentToolPermission(
        SetAgentToolPermissionInput input,
        IResolverContext context,
        [Service] IAgentToolPermissionRepository permissions,
        CancellationToken ct)
    {
        _ = DashboardAuthContextExtensions.GetUser(context);
        await permissions.UpsertAsync(input.AgentId, input.Skill, input.Tool, input.Mode, ct);
        return new ToolPermissionPayload(input.Skill, input.Tool, input.Mode);
    }

    [GraphQLDescription("Replaces explicit tool permission overrides for an agent.")]
    public async Task<IReadOnlyList<ToolPermissionPayload>> SetAgentToolPermissions(
        SetAgentToolPermissionsInput input,
        IResolverContext context,
        [Service] IAgentToolPermissionRepository permissions,
        CancellationToken ct)
    {
        _ = DashboardAuthContextExtensions.GetUser(context);
        var rows = input.Entries.Select(e => new AgentToolPermissionRecord
        {
            AgentId = input.AgentId,
            SkillName = e.Skill,
            ToolName = e.Tool,
            Permission = e.Mode,
        }).ToList();

        await permissions.SetManyAsync(input.AgentId, rows, ct);
        return rows.Select(p => new ToolPermissionPayload(p.SkillName, p.ToolName, p.Permission)).ToList();
    }
}
