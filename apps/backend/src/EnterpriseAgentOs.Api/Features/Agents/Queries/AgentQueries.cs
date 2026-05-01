namespace EnterpriseAgentOs.Api.Features.Agents;

[ExtendObjectType(typeof(GraphQLQueries))]
public class AgentQueries
{
    private static readonly TimeSpan CacheTtl = TimeSpan.FromSeconds(30);
    private const string ListCacheKey = "agents:dashboard:list";
    private static string DetailCacheKey(Guid id) => $"agents:dashboard:{id}";

    [GraphQLDescription("Lists all agents owned by the authenticated user with id, name, provider, model, status, and pod info.")]
    public async Task<IReadOnlyList<AgentDto>> GetAgents(
        IResolverContext context,
        [Service] IAgentService agents,
        [Service] IMemoryCache cache,
        CancellationToken ct)
    {
        _ = DashboardAuthContextExtensions.GetUser(context);

        if (cache.TryGetValue(ListCacheKey, out IReadOnlyList<AgentDto>? cached) && cached is not null)
            return cached;

        var result = await agents.ListAsync(ct);
        cache.Set(ListCacheKey, result, new MemoryCacheEntryOptions { AbsoluteExpirationRelativeToNow = CacheTtl });
        return result;
    }

    [GraphQLDescription("Returns a single agent by ID including its full aggregate: personality files, installed skills, memories, channel bindings, and cron jobs.")]
    public async Task<AgentRecord?> GetAgent(
        Guid id,
        IResolverContext context,
        [Service] IAgentRepository agents,
        [Service] IMemoryCache cache,
        CancellationToken ct)
    {
        _ = DashboardAuthContextExtensions.GetUser(context);

        var key = DetailCacheKey(id);
        if (cache.TryGetValue(key, out AgentRecord? cached) && cached is not null)
            return cached;

        var result = await agents.GetAsync(id, ct);
        if (result is not null)
            cache.Set(key, result, new MemoryCacheEntryOptions { AbsoluteExpirationRelativeToNow = CacheTtl });

        return result;
    }

    [GraphQLDescription("Returns explicit allow/deny tool permission overrides for an agent.")]
    public async Task<IReadOnlyList<ToolPermissionPayload>> GetAgentToolPermissions(
        Guid agentId,
        IResolverContext context,
        [Service] IAgentToolPermissionRepository permissions,
        CancellationToken ct)
    {
        _ = DashboardAuthContextExtensions.GetUser(context);
        var rows = await permissions.ListForAgentAsync(agentId, ct);
        return rows.Select(p => new ToolPermissionPayload(p.SkillName, p.ToolName, p.Permission)).ToList();
    }

    [GraphQLDescription("Returns the backend-owned tool catalog for dashboard permission UIs.")]
    public async Task<IReadOnlyList<AgentToolCatalogEntry>> GetAgentToolCatalog(
        Guid? agentId,
        IResolverContext context,
        [Service] IAgentToolCatalogService catalog,
        CancellationToken ct)
    {
        _ = DashboardAuthContextExtensions.GetUser(context);
        return await catalog.ListAsync(agentId, ct);
    }

    [GraphQLDescription("Lists persisted runs for an agent, optionally filtered by parent run.")]
    public async Task<IReadOnlyList<AgentRunRecord>> GetAgentRuns(
        Guid agentId,
        Guid? parentRunId,
        IResolverContext context,
        [Service] IAgentRunRepository runs,
        CancellationToken ct)
    {
        _ = DashboardAuthContextExtensions.GetUser(context);
        return await runs.ListForAgentAsync(agentId, parentRunId, ct);
    }
}
