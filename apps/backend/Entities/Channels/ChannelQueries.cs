namespace EnterpriseAgentOs.Api.Queries;

[ExtendObjectType(typeof(EnterpriseAgentOs.Api.GraphQLQueries))]
public class ChannelQueries
{
    public async Task<IReadOnlyList<EnterpriseAgentOs.Api.Entities.Channels.Types.ChannelConnectionGqlDto>> GetChannelConnections(
        IResolverContext context,
        [Service] EnterpriseAgentOs.Api.Entities.Channels.IChannelRepository repo,
        CancellationToken ct)
    {
        _ = EnterpriseAgentOs.Api.Middleware.DashboardAuthContextExtensions.GetUser(context);
        var rows = await repo.ListConnectionsAsync(ct);
        return rows.Select(EnterpriseAgentOs.Api.Entities.Channels.Types.ChannelGraphQLMapper.ToDto).ToList();
    }

    public async Task<EnterpriseAgentOs.Api.Entities.Channels.Types.ChannelConnectionGqlDto?> GetChannelConnection(
        Guid id,
        IResolverContext context,
        [Service] EnterpriseAgentOs.Api.Entities.Channels.IChannelRepository repo,
        CancellationToken ct)
    {
        _ = EnterpriseAgentOs.Api.Middleware.DashboardAuthContextExtensions.GetUser(context);
        var row = await repo.GetConnectionAsync(id, ct);
        return row is null ? null : EnterpriseAgentOs.Api.Entities.Channels.Types.ChannelGraphQLMapper.ToDto(row);
    }

    public IReadOnlyList<EnterpriseAgentOs.Api.Entities.Channels.Types.ChannelTypeGqlDto> GetChannelTypes(
        IResolverContext context)
    {
        _ = EnterpriseAgentOs.Api.Middleware.DashboardAuthContextExtensions.GetUser(context);
        return EnterpriseAgentOs.Api.Entities.Channels.ChannelTypes.All
            .Select(t => new EnterpriseAgentOs.Api.Entities.Channels.Types.ChannelTypeGqlDto(t.Type, t.DisplayName, t.Description))
            .ToList();
    }

    public async Task<IReadOnlyList<EnterpriseAgentOs.Api.Entities.Channels.Types.AgentChannelBindingGqlDto>> GetAgentChannelBindings(
        Guid agentId,
        IResolverContext context,
        [Service] EnterpriseAgentOs.Api.Entities.Channels.IChannelRepository repo,
        CancellationToken ct)
    {
        _ = EnterpriseAgentOs.Api.Middleware.DashboardAuthContextExtensions.GetUser(context);
        var rows = await repo.ListBindingsAsync(agentId, ct);
        return rows.Select(EnterpriseAgentOs.Api.Entities.Channels.Types.ChannelGraphQLMapper.ToDto).ToList();
    }
}
