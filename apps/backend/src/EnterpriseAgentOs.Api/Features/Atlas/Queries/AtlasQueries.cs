using EnterpriseAgentOs.Domain.Features.Atlas;

namespace EnterpriseAgentOs.Api.Features.Atlas;

[ExtendObjectType(typeof(GraphQLQueries))]
public sealed class AtlasQueries
{
    public Task<IReadOnlyList<AtlasConnectorTypeRecord>> GetAtlasConnectorTypes(
        IResolverContext context,
        [Service] IAtlasService service,
        CancellationToken ct)
    {
        _ = DashboardAuthContextExtensions.GetUser(context);
        return service.ListConnectorTypesAsync(ct);
    }

    public Task<IReadOnlyList<AtlasConnectorConnectionRecord>> GetAtlasConnections(
        IResolverContext context,
        [Service] IAtlasService service,
        CancellationToken ct)
    {
        _ = DashboardAuthContextExtensions.GetUser(context);
        return service.ListConnectionsAsync(ct);
    }

    public Task<AtlasConnectorConnectionRecord?> GetAtlasConnection(
        Guid id,
        IResolverContext context,
        [Service] IAtlasService service,
        CancellationToken ct)
    {
        _ = DashboardAuthContextExtensions.GetUser(context);
        return service.GetConnectionAsync(id, ct);
    }

    public Task<IReadOnlyList<AtlasRequestHistoryRecord>> GetAtlasRequestHistory(
        Guid? connectionId,
        IResolverContext context,
        [Service] IAtlasService service,
        CancellationToken ct)
    {
        _ = DashboardAuthContextExtensions.GetUser(context);
        return service.ListHistoryAsync(connectionId, ct);
    }
}
