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

    public Task<IReadOnlyList<AtlasActivityRecord>> GetAtlasActivity(
        Guid? connectionId,
        IResolverContext context,
        [Service] IAtlasService service,
        CancellationToken ct)
    {
        _ = DashboardAuthContextExtensions.GetUser(context);
        return service.ListActivityAsync(connectionId, ct);
    }

    public Task<IReadOnlyList<AtlasIndexJobRecord>> GetAtlasIndexJobs(
        Guid connectionId,
        int? limit,
        IResolverContext context,
        [Service] IAtlasService service,
        CancellationToken ct)
    {
        _ = DashboardAuthContextExtensions.GetUser(context);
        return service.ListIndexJobsAsync(connectionId, Math.Clamp(limit ?? 20, 1, 100), ct);
    }

    public Task<AtlasIndexedRecordPage> GetAtlasIndexedRecords(
        Guid connectionId,
        string entity,
        string? query,
        string? cursor,
        int? limit,
        IResolverContext context,
        [Service] IAtlasService service,
        CancellationToken ct)
    {
        _ = DashboardAuthContextExtensions.GetUser(context);
        return service.SearchRecordsAsync(new AtlasIndexedRecordFilter
        {
            ConnectionId = connectionId,
            Entity = entity,
            Query = query,
            Cursor = cursor,
            Limit = Math.Clamp(limit ?? 20, 1, 100),
        }, ct);
    }
}
