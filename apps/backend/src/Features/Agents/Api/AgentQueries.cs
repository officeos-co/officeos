namespace EnterpriseAgentOs.Api.Features.Agents;

[ExtendObjectType(typeof(GraphQLQueries))]
public class AgentQueries
{
    private static readonly TimeSpan CacheTtl = TimeSpan.FromSeconds(30);
    [GraphQLDescription("Lists all agents owned by the authenticated user with id, name, provider, model, status, and pod info.")]
    public async Task<IReadOnlyList<AgentDto>> GetAgents(
        [Service] UserContext user,
        [Service] IAgentService agents,
        [Service] IDistributedCache cache,
        CancellationToken ct)
    {
        var listCacheKey = AgentCacheKeys.DashboardList(user.Id);
        var cached = await cache.GetJsonAsync<IReadOnlyList<AgentDto>>(listCacheKey, ct);
        if (cached is not null)
            return cached;

        var result = await agents.ListAsync(new AgentFilter { OwnerId = user.Id }, ct);
        await cache.SetJsonAsync(listCacheKey, result, CacheTtl, ct);
        return result;
    }

    [GraphQLDescription("Returns a single agent by ID including its full aggregate: personality files, installed skills, memories, channel bindings, and cron jobs.")]
    public async Task<AgentRecord?> GetAgent(
        Guid id,
        [Service] UserContext user,
        [Service] IAgentRepository agents,
        [Service] IDistributedCache cache,
        CancellationToken ct)
    {
        var key = AgentCacheKeys.DashboardDetail(id, user.Id);
        var cached = await cache.GetJsonAsync<AgentRecord>(key, ct);
        if (cached is not null)
            return cached;

        var result = await agents.GetByAsync(new AgentFilter { Id = id, OwnerId = user.Id }, ct);
        if (result is not null)
            await cache.SetJsonAsync(key, result, CacheTtl, ct);

        return result;
    }

    [GraphQLDescription("Returns explicit allow/deny tool permission overrides for an agent.")]
    public async Task<IReadOnlyList<ToolPermissionPayload>> GetAgentToolPermissions(
        Guid agentId,
        [Service] UserContext user,
        [Service] IAgentDashboardService agents,
        CancellationToken ct)
    {
        var rows = await agents.ListToolPermissionsAsync(user.Id, agentId, ct);
        return rows.Select(p => new ToolPermissionPayload(p.SkillName, p.ToolName, p.Permission)).ToList();
    }

    [GraphQLDescription("Returns the backend-owned tool catalog for dashboard permission UIs.")]
    public async Task<IReadOnlyList<AgentToolCatalogEntry>> GetAgentToolCatalog(
        Guid? agentId,
        [Service] IAgentToolCatalogService catalog,
        CancellationToken ct)
    {
        return await catalog.ListAsync(agentId, ct);
    }

    [GraphQLDescription("Lists persisted runs for an agent, optionally filtered by parent run.")]
    public async Task<IReadOnlyList<AgentRunRecord>> GetAgentRuns(
        Guid agentId,
        Guid? parentRunId,
        [Service] UserContext user,
        [Service] IAgentDashboardService agents,
        CancellationToken ct)
    {
        return await agents.ListRunsAsync(user.Id, agentId, parentRunId, ct);
    }
}
