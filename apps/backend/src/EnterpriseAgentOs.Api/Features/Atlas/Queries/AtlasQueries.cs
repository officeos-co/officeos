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
        AtlasConnectionFilter? filter,
        IResolverContext context,
        [Service] IAtlasService service,
        CancellationToken ct)
    {
        _ = DashboardAuthContextExtensions.GetUser(context);
        return service.ListAsync(filter ?? new AtlasConnectionFilter(), ct);
    }

    public Task<AtlasConnectorConnectionRecord?> GetAtlasConnection(
        AtlasConnectionFilter filter,
        IResolverContext context,
        [Service] IAtlasService service,
        CancellationToken ct)
    {
        _ = DashboardAuthContextExtensions.GetUser(context);
        return service.GetByAsync(filter, ct);
    }

    public Task<IReadOnlyList<AtlasRequestHistoryRecord>> GetAtlasRequestHistory(
        AtlasRequestHistoryFilter? filter,
        IResolverContext context,
        [Service] IAtlasService service,
        CancellationToken ct)
    {
        _ = DashboardAuthContextExtensions.GetUser(context);
        return service.ListAsync(filter ?? new AtlasRequestHistoryFilter(), ct);
    }

    public Task<IReadOnlyList<AtlasActivityRecord>> GetAtlasActivity(
        AtlasActivityFilter? filter,
        IResolverContext context,
        [Service] IAtlasService service,
        CancellationToken ct)
    {
        _ = DashboardAuthContextExtensions.GetUser(context);
        return service.ListAsync(filter ?? new AtlasActivityFilter(), ct);
    }

    public Task<IReadOnlyList<AtlasIndexJobRecord>> GetAtlasIndexJobs(
        AtlasIndexJobFilter filter,
        IResolverContext context,
        [Service] IAtlasService service,
        CancellationToken ct)
    {
        _ = DashboardAuthContextExtensions.GetUser(context);
        return service.ListAsync(filter, ct);
    }

    public Task<AtlasIndexedRecordRecord?> GetAtlasIndexedRecord(
        AtlasIndexedRecordFilter filter,
        IResolverContext context,
        [Service] IAtlasService service,
        CancellationToken ct)
    {
        _ = DashboardAuthContextExtensions.GetUser(context);
        return service.GetByAsync(filter, ct);
    }

    public Task<AtlasIndexedRecordPage> GetAtlasIndexedRecords(
        AtlasIndexedRecordFilter filter,
        IResolverContext context,
        [Service] IAtlasService service,
        CancellationToken ct)
    {
        _ = DashboardAuthContextExtensions.GetUser(context);
        return service.SearchAsync(filter, ct);
    }
}
