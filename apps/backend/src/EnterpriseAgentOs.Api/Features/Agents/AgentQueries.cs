namespace EnterpriseAgentOs.Api.Features.Agents;

[ExtendObjectType(typeof(GraphQLQueries))]
public class AgentQueries
{
    private static readonly TimeSpan CacheTtl = TimeSpan.FromSeconds(30);
    private const string ListCacheKey = "agents:dashboard:list";
    private static string DetailCacheKey(Guid id) => $"agents:dashboard:{id}";

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
}
