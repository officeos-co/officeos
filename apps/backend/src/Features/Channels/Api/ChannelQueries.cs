namespace OffceOs.Api.Features.Channels;

[ExtendObjectType(typeof(GraphQLQueries))]
public class ChannelQueries
{
    private static readonly TimeSpan ChannelCacheTtl = TimeSpan.FromMinutes(5);
    private static string ChannelListCacheKey(Guid userId) => $"channels:list:{userId}";
    private static string ChannelCacheKey(Guid id, Guid userId) => $"channels:{id}:user:{userId}";

    [GraphQLDescription("Lists all channel connections (Slack, Telegram, Discord, etc.) configured by the user.")]
    public async Task<IReadOnlyList<ChannelConnectionPayload>> GetChannelConnections(
        [Service] UserContext user,
        [Service] IChannelRepository repo,
        [Service] IDistributedCache cache,
        CancellationToken ct)
    {
        var listKey = ChannelListCacheKey(user.Id);
        var cached = await cache.GetJsonAsync<IReadOnlyList<ChannelConnectionPayload>>(listKey, ct);
        if (cached is not null)
            return cached;

        var rows = await repo.ListConnectionsAsync(new ChannelConnectionFilter { CreatedById = user.Id }, ct);
        var result = rows.Select(ChannelGraphQLMapper.ToPayload).ToList();

        await cache.SetJsonAsync(listKey, (IReadOnlyList<ChannelConnectionPayload>)result, ChannelCacheTtl, ct);
        return result;
    }

    [GraphQLDescription("Returns a single channel connection by ID.")]
    public async Task<ChannelConnectionPayload?> GetChannelConnection(
        Guid id,
        [Service] UserContext user,
        [Service] IChannelRepository repo,
        [Service] IDistributedCache cache,
        CancellationToken ct)
    {
        var key = ChannelCacheKey(id, user.Id);
        var cached = await cache.GetJsonAsync<ChannelConnectionPayload>(key, ct);
        if (cached is not null)
            return cached;

        var row = await repo.GetConnectionByAsync(new ChannelConnectionFilter { Id = id, CreatedById = user.Id }, ct);
        if (row is null) return null;
        var dto = ChannelGraphQLMapper.ToPayload(row);

        await cache.SetJsonAsync(key, dto, ChannelCacheTtl, ct);
        return dto;
    }

    [GraphQLDescription("Returns all supported channel types with display names, descriptions, logos, and onboarding step definitions.")]
    public IReadOnlyList<ChannelTypeDefinition> GetChannelKinds()
    {
        return ChannelKinds.All;
    }

    [GraphQLDescription("Lists all channel bindings for a specific agent showing which channels the agent listens on.")]
    public async Task<IReadOnlyList<AgentChannelBindingPayload>> GetAgentChannelBindings(
        Guid agentId,
        [Service] UserContext user,
        [Service] IChannelService channels,
        CancellationToken ct)
    {
        var rows = await channels.ListBindingsForOwnedAgentAsync(agentId, user.Id, ct);
        return rows.Select(ChannelGraphQLMapper.ToPayload).ToList();
    }
}
