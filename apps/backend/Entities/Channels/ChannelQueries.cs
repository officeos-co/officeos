using HotChocolate.Resolvers;

namespace EnterpriseAgentOs.Api.Queries;

[ExtendObjectType(typeof(GraphQLQueries))]
public class ChannelQueries
{
    public async Task<IReadOnlyList<ChannelConnectionGqlDto>> GetChannelConnections(
        IResolverContext context,
        [Service] IChannelRepository repo,
        CancellationToken ct)
    {
        _ = DashboardAuthContextExtensions.GetUser(context);
        var rows = await repo.ListConnectionsAsync(ct);
        return rows.Select(ChannelGraphQLMapper.ToDto).ToList();
    }

    public async Task<ChannelConnectionGqlDto?> GetChannelConnection(
        Guid id,
        IResolverContext context,
        [Service] IChannelRepository repo,
        CancellationToken ct)
    {
        _ = DashboardAuthContextExtensions.GetUser(context);
        var row = await repo.GetConnectionAsync(id, ct);
        return row is null ? null : ChannelGraphQLMapper.ToDto(row);
    }

    public IReadOnlyList<ChannelTypeGqlDto> GetChannelTypes(
        IResolverContext context)
    {
        _ = DashboardAuthContextExtensions.GetUser(context);
        return ChannelTypes.All
            .Select(t => new ChannelTypeGqlDto(t.Type, t.DisplayName, t.Description))
            .ToList();
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
