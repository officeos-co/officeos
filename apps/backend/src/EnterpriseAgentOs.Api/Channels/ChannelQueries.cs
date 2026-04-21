namespace EnterpriseAgentOs.Api.Channels;

[ExtendObjectType(typeof(GraphQLQueries))]
public class ChannelQueries
{
    private static readonly TimeSpan ChannelCacheTtl = TimeSpan.FromMinutes(5);
    private const string ChannelListCacheKey = "channels:list";
    private static string ChannelCacheKey(Guid id) => $"channels:{id}";

    public async Task<IReadOnlyList<ChannelConnectionGqlDto>> GetChannelConnections(
        IResolverContext context,
        [Service] IChannelRepository repo,
        [Service] IMemoryCache cache,
        CancellationToken ct)
    {
        _ = DashboardAuthContextExtensions.GetUser(context);

        if (cache.TryGetValue(ChannelListCacheKey, out IReadOnlyList<ChannelConnectionGqlDto>? cached) && cached is not null)
            return cached;

        var rows = await repo.ListConnectionsAsync(ct);
        var result = rows.Select(ChannelGraphQLMapper.ToDto).ToList();

        cache.Set(ChannelListCacheKey, (IReadOnlyList<ChannelConnectionGqlDto>)result,
            new MemoryCacheEntryOptions
            { AbsoluteExpirationRelativeToNow = ChannelCacheTtl });
        return result;
    }

    public async Task<ChannelConnectionGqlDto?> GetChannelConnection(
        Guid id,
        IResolverContext context,
        [Service] IChannelRepository repo,
        [Service] IMemoryCache cache,
        CancellationToken ct)
    {
        _ = DashboardAuthContextExtensions.GetUser(context);

        var key = ChannelCacheKey(id);
        if (cache.TryGetValue(key, out ChannelConnectionGqlDto? cached) && cached is not null)
            return cached;

        var row = await repo.GetConnectionAsync(id, ct);
        if (row is null) return null;
        var dto = ChannelGraphQLMapper.ToDto(row);

        cache.Set(key, dto,
            new MemoryCacheEntryOptions
            { AbsoluteExpirationRelativeToNow = ChannelCacheTtl });
        return dto;
    }

    public IReadOnlyList<ChannelTypeDefinition> GetChannelTypes(
        IResolverContext context)
    {
        _ = DashboardAuthContextExtensions.GetUser(context);
        return ChannelTypes.All;
    }

    public async Task<IReadOnlyList<AgentChannelBindingGqlDto>> GetAgentChannelBindings(
        Guid agentId,
        IResolverContext context,
        [Service] IChannelRepository repo,
        CancellationToken ct)
    {
        _ = DashboardAuthContextExtensions.GetUser(context);
        var rows = await repo.ListBindingsAsync(agentId, ct);
        return rows.Select(ChannelGraphQLMapper.ToDto).ToList();
    }
}
