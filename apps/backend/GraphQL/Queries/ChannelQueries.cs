namespace EnterpriseAgentOs.Api.GraphQL.Queries;

[ExtendObjectType(typeof(EnterpriseAgentOs.Api.GraphQLQueries))]
public class ChannelQueries
{
    public async Task<IReadOnlyList<EnterpriseAgentOs.Api.GraphQL.Types.ChannelConnectionGqlDto>> GetChannelConnections(
        IResolverContext context,
        [Service] EnterpriseAgentOs.Domain.Interfaces.Channels.IChannelRepository repo,
        CancellationToken ct)
    {
        _ = EnterpriseAgentOs.Api.Middleware.DashboardAuthContextExtensions.GetUser(context);
        var rows = await repo.ListConnectionsAsync(ct);
        return rows.Select(EnterpriseAgentOs.Api.GraphQL.Types.ChannelGraphQLMapper.ToDto).ToList();
    }

    public async Task<EnterpriseAgentOs.Api.GraphQL.Types.ChannelConnectionGqlDto?> GetChannelConnection(
        Guid id,
        IResolverContext context,
        [Service] EnterpriseAgentOs.Domain.Interfaces.Channels.IChannelRepository repo,
        CancellationToken ct)
    {
        _ = EnterpriseAgentOs.Api.Middleware.DashboardAuthContextExtensions.GetUser(context);
        var row = await repo.GetConnectionAsync(id, ct);
        return row is null ? null : EnterpriseAgentOs.Api.GraphQL.Types.ChannelGraphQLMapper.ToDto(row);
    }

    public IReadOnlyList<EnterpriseAgentOs.Api.GraphQL.Types.ChannelTypeGqlDto> GetChannelTypes(
        IResolverContext context)
    {
        _ = EnterpriseAgentOs.Api.Middleware.DashboardAuthContextExtensions.GetUser(context);
        return EnterpriseAgentOs.Domain.DTOs.Channels.ChannelTypes.All
            .Select(t => new EnterpriseAgentOs.Api.GraphQL.Types.ChannelTypeGqlDto(t.Type, t.DisplayName, t.Description))
            .ToList();
    }

    public async Task<IReadOnlyList<EnterpriseAgentOs.Api.GraphQL.Types.AgentChannelBindingGqlDto>> GetAgentChannelBindings(
        Guid agentId,
        IResolverContext context,
        [Service] EnterpriseAgentOs.Domain.Interfaces.Channels.IChannelRepository repo,
        CancellationToken ct)
    {
        _ = EnterpriseAgentOs.Api.Middleware.DashboardAuthContextExtensions.GetUser(context);
        var rows = await repo.ListBindingsAsync(agentId, ct);
        return rows.Select(EnterpriseAgentOs.Api.GraphQL.Types.ChannelGraphQLMapper.ToDto).ToList();
    }
}
