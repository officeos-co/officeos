namespace EnterpriseAgentOs.Api.GraphQL.Queries;

[ExtendObjectType(typeof(GraphQLQueries))]
public class ChannelQueries
{
    private static readonly TimeSpan ChannelCacheTtl = TimeSpan.FromMinutes(5);
    private const string ChannelListCacheKey = "channels:list";
    private static string ChannelCacheKey(Guid id) => $"channels:{id}";

    public async Task<IReadOnlyList<Types.ChannelConnectionGqlDto>> GetChannelConnections(
        IResolverContext context,
        [Service] IChannelRepository repo,
        [Service] IMemoryCache cache,
        CancellationToken ct)
    {
        _ = Middleware.DashboardAuthContextExtensions.GetUser(context);

        if (cache.TryGetValue(ChannelListCacheKey, out IReadOnlyList<Types.ChannelConnectionGqlDto>? cached) && cached is not null)
            return cached;

        var rows = await repo.ListConnectionsAsync(ct);
        var result = rows.Select(Types.ChannelGraphQLMapper.ToDto).ToList();

        cache.Set(ChannelListCacheKey, (IReadOnlyList<Types.ChannelConnectionGqlDto>)result,
            new MemoryCacheEntryOptions
            { AbsoluteExpirationRelativeToNow = ChannelCacheTtl });
        return result;
    }

    public async Task<Types.ChannelConnectionGqlDto?> GetChannelConnection(
        Guid id,
        IResolverContext context,
        [Service] IChannelRepository repo,
        [Service] IMemoryCache cache,
        CancellationToken ct)
    {
        _ = Middleware.DashboardAuthContextExtensions.GetUser(context);

        var key = ChannelCacheKey(id);
        if (cache.TryGetValue(key, out Types.ChannelConnectionGqlDto? cached) && cached is not null)
            return cached;

        var row = await repo.GetConnectionAsync(id, ct);
        if (row is null) return null;
        var dto = Types.ChannelGraphQLMapper.ToDto(row);

        cache.Set(key, dto,
            new MemoryCacheEntryOptions
            { AbsoluteExpirationRelativeToNow = ChannelCacheTtl });
        return dto;
    }

    public IReadOnlyList<Types.ChannelTypeGqlDto> GetChannelTypes(
        IResolverContext context)
    {
        _ = Middleware.DashboardAuthContextExtensions.GetUser(context);
        return ChannelTypes.All
            .Select(t => new Types.ChannelTypeGqlDto(
                t.Type, t.DisplayName, t.Description, t.Logo,
                t.OnboardingSteps.Select(s => new Types.OnboardingStepGqlDto(
                    s.Type, s.Title, s.Description, s.Value,
                    s.InputKey, s.InputLabel, s.InputPlaceholder, s.InputHelp,
                    s.InputKind, s.InputRequired)).ToList()))
            .ToList();
    }

    public async Task<IReadOnlyList<Types.AgentChannelBindingGqlDto>> GetAgentChannelBindings(
        Guid agentId,
        IResolverContext context,
        [Service] IChannelRepository repo,
        CancellationToken ct)
    {
        _ = Middleware.DashboardAuthContextExtensions.GetUser(context);
        var rows = await repo.ListBindingsAsync(agentId, ct);
        return rows.Select(Types.ChannelGraphQLMapper.ToDto).ToList();
    }
}
