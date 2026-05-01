namespace EnterpriseAgentOs.Api.Features.Agents;

[ExtendObjectType(typeof(GraphQLMutations))]
public class AgentDashboardMutations
{
    private const string AgentListQueryCacheKey = "agents:dashboard:list";
    private static string AgentQueryCacheKey(Guid id) => $"agents:dashboard:{id}";

    [GraphQLDescription("Creates a new agent with the given config. Optionally assigns skills, tool permissions, and channels.")]
    public async Task<AgentDto> CreateAgent(
        CreateAgentInput input,
        IResolverContext context,
        [Service] IAgentService agents,
        [Service] IMemoryCache cache,
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

        await agents.InitializeAgentAsync(
            dto.Id,
            user.Id,
            new AgentInitRequest(
                toolNames,
                input.ToolPermissions?.Select(tp => new AgentToolPermissionInit(tp.Tool, tp.Mode)).ToList(),
                input.ChannelSlugs,
                bootstrap),
            ct);

        cache.Remove(AgentListQueryCacheKey);
        return dto;
    }

    [GraphQLDescription("Patches mutable fields on an existing agent (name, provider, model, prompt). Null fields are left unchanged.")]
    public async Task<AgentDto> UpdateAgent(
        Guid id,
        UpdateAgentInput input,
        IResolverContext context,
        [Service] IAgentService agents,
        [Service] IMemoryCache cache,
        CancellationToken ct)
    {
        _ = DashboardAuthContextExtensions.GetUser(context);
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
        cache.Remove(AgentListQueryCacheKey);
        cache.Remove(AgentQueryCacheKey(id));
        return dto;
    }

    [GraphQLDescription("Soft-deletes an agent and removes its Kubernetes pod.")]
    public async Task<bool> DeleteAgent(
        Guid id,
        IResolverContext context,
        [Service] IAgentService agents,
        [Service] IBrowserService browser,
        [Service] IMemoryCache cache,
        CancellationToken ct)
    {
        _ = DashboardAuthContextExtensions.GetUser(context);
        await browser.StopAsync(id, ct);
        var result = await agents.DeleteAsync(id, ct);
        cache.Remove(AgentListQueryCacheKey);
        cache.Remove(AgentQueryCacheKey(id));
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
