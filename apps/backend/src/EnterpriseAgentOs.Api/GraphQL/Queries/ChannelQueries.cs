namespace EnterpriseAgentOs.Api.GraphQL.Queries;

[ExtendObjectType(typeof(GraphQLQueries))]
public class ChannelQueries
{
    public async Task<IReadOnlyList<Types.ChannelConnectionGqlDto>> GetChannelConnections(
        IResolverContext context,
        [Service] IChannelRepository repo,
        CancellationToken ct)
    {
        _ = Middleware.DashboardAuthContextExtensions.GetUser(context);
        var rows = await repo.ListConnectionsAsync(ct);
        return rows.Select(Types.ChannelGraphQLMapper.ToDto).ToList();
    }

    public async Task<Types.ChannelConnectionGqlDto?> GetChannelConnection(
        Guid id,
        IResolverContext context,
        [Service] IChannelRepository repo,
        CancellationToken ct)
    {
        _ = Middleware.DashboardAuthContextExtensions.GetUser(context);
        var row = await repo.GetConnectionAsync(id, ct);
        return row is null ? null : Types.ChannelGraphQLMapper.ToDto(row);
    }

    public IReadOnlyList<Types.ChannelTypeGqlDto> GetChannelTypes(
        IResolverContext context)
    {
        _ = Middleware.DashboardAuthContextExtensions.GetUser(context);
        return ChannelTypes.All
            .Select(t => new Types.ChannelTypeGqlDto(t.Type, t.DisplayName, t.Description, t.Logo))
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
