namespace EnterpriseAgentOs.Api.Features.Agents;

[ExtendObjectType(typeof(GraphQLQueries))]
public class ChannelQueries
{
    private static readonly TimeSpan ChannelCacheTtl = TimeSpan.FromMinutes(5);
    private const string ChannelListCacheKey = "channels:list";
    private static string ChannelCacheKey(Guid id) => $"channels:{id}";

    [GraphQLDescription("Lists all channel connections (Slack, Telegram, Discord, etc.) configured by the user.")]
    public async Task<IReadOnlyList<ChannelConnectionGqlDto>> GetChannelConnections(
        IResolverContext context,
        [Service] IChannelRepository repo,
        [Service] IDistributedCache cache,
        CancellationToken ct)
    {
        _ = DashboardAuthContextExtensions.GetUser(context);

        var cached = await cache.GetJsonAsync<IReadOnlyList<ChannelConnectionGqlDto>>(ChannelListCacheKey, ct);
        if (cached is not null)
            return cached;

        var rows = await repo.ListConnectionsAsync(ct);
        var result = rows.Select(ChannelGraphQLMapper.ToDto).ToList();

        await cache.SetJsonAsync(ChannelListCacheKey, (IReadOnlyList<ChannelConnectionGqlDto>)result, ChannelCacheTtl, ct);
        return result;
    }

    [GraphQLDescription("Returns a single channel connection by ID.")]
    public async Task<ChannelConnectionGqlDto?> GetChannelConnection(
        Guid id,
        IResolverContext context,
        [Service] IChannelRepository repo,
        [Service] IDistributedCache cache,
        CancellationToken ct)
    {
        _ = DashboardAuthContextExtensions.GetUser(context);

        var key = ChannelCacheKey(id);
        var cached = await cache.GetJsonAsync<ChannelConnectionGqlDto>(key, ct);
        if (cached is not null)
            return cached;

        var row = await repo.GetConnectionAsync(id, ct);
        if (row is null) return null;
        var dto = ChannelGraphQLMapper.ToDto(row);

        await cache.SetJsonAsync(key, dto, ChannelCacheTtl, ct);
        return dto;
    }

    [GraphQLDescription("Returns all supported channel types with display names, descriptions, logos, and onboarding step definitions.")]
    public IReadOnlyList<ChannelTypeDefinition> GetChannelTypes(
        IResolverContext context)
    {
        _ = DashboardAuthContextExtensions.GetUser(context);
        return ChannelTypes.All;
    }

    [GraphQLDescription("Lists all channel bindings for a specific agent showing which channels the agent listens on.")]
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
