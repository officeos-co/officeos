namespace EnterpriseAgentOs.Api.Features.Agents;

[ExtendObjectType(typeof(GraphQLQueries))]
public class AgentQueries
{
    private static readonly TimeSpan AgentQueryCacheTtl = TimeSpan.FromSeconds(30);
    private const string AgentListQueryCacheKey = "agents:dashboard:list";
    private static string AgentQueryCacheKey(Guid id) => $"agents:dashboard:{id}";

    public async Task<IReadOnlyList<AgentDto>> GetAgents(
        IResolverContext context,
        [Service] IAgentService agents,
        [Service] IMemoryCache cache,
        CancellationToken ct)
    {
        _ = DashboardAuthContextExtensions.GetUser(context);

        if (cache.TryGetValue(AgentListQueryCacheKey, out IReadOnlyList<AgentDto>? cached) && cached is not null)
            return cached;

        var result = await agents.ListAsync(ct);

        cache.Set(AgentListQueryCacheKey, result,
            new MemoryCacheEntryOptions { AbsoluteExpirationRelativeToNow = AgentQueryCacheTtl });
        return result;
    }

    public async Task<IReadOnlyList<AgentMemoryRecord>> GetAgentMemories(
        Guid agentId,
        IResolverContext context,
        [Service] IAgentMemoryRepository memories,
        CancellationToken ct)
    {
        _ = DashboardAuthContextExtensions.GetUser(context);
        return await memories.ListAsync(agentId, ct);
    }

    public async Task<AgentDto?> GetAgent(
        Guid id,
        IResolverContext context,
        [Service] IAgentService agents,
        [Service] IMemoryCache cache,
        CancellationToken ct)
    {
        _ = DashboardAuthContextExtensions.GetUser(context);

        var key = AgentQueryCacheKey(id);
        if (cache.TryGetValue(key, out AgentDto? cached) && cached is not null)
            return cached;

        var result = await agents.GetAsync(id, ct);
        if (result is not null)
        {
            cache.Set(key, result,
                new MemoryCacheEntryOptions { AbsoluteExpirationRelativeToNow = AgentQueryCacheTtl });
        }
        return result;
    }
}
