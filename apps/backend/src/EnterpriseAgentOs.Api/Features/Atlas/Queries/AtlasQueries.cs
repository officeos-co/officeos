using EnterpriseAgentOs.Domain.Features.Atlas;

namespace EnterpriseAgentOs.Api.Features.Atlas;

[ExtendObjectType(typeof(GraphQLQueries))]
public sealed class AtlasQueries
{
    public Task<IReadOnlyList<AtlasConnectorTypeRecord>> GetAtlasConnectorTypes(
        [Service] IAtlasService service,
        CancellationToken ct)
    {
        return service.ListConnectorTypesAsync(ct);
    }

    public Task<IReadOnlyList<AtlasConnectorConnectionRecord>> GetAtlasConnections(
        AtlasConnectionFilter? filter,
        [Service] IAtlasService service,
        CancellationToken ct)
    {
        return service.ListAsync(filter ?? new AtlasConnectionFilter(), ct);
    }

    public Task<AtlasConnectorConnectionRecord?> GetAtlasConnection(
        AtlasConnectionFilter filter,
        [Service] IAtlasService service,
        CancellationToken ct)
    {
        return service.GetByAsync(filter, ct);
    }

    public Task<IReadOnlyList<AtlasRequestHistoryRecord>> GetAtlasRequestHistory(
        AtlasRequestHistoryFilter? filter,
        [Service] IAtlasService service,
        CancellationToken ct)
    {
        return service.ListAsync(filter ?? new AtlasRequestHistoryFilter(), ct);
    }

    public Task<IReadOnlyList<AtlasActivityRecord>> GetAtlasActivity(
        AtlasActivityFilter? filter,
        [Service] IAtlasService service,
        CancellationToken ct)
    {
        return service.ListAsync(filter ?? new AtlasActivityFilter(), ct);
    }

    public Task<IReadOnlyList<AtlasIndexJobRecord>> GetAtlasIndexJobs(
        AtlasIndexJobFilter filter,
        [Service] IAtlasService service,
        CancellationToken ct)
    {
        return service.ListAsync(filter, ct);
    }

    public Task<AtlasIndexedRecordRecord?> GetAtlasIndexedRecord(
        AtlasIndexedRecordFilter filter,
        [Service] IAtlasService service,
        CancellationToken ct)
    {
        return service.GetByAsync(filter, ct);
    }

    public Task<AtlasIndexedRecordPage> GetAtlasIndexedRecords(
        AtlasIndexedRecordFilter filter,
        [Service] IAtlasService service,
        CancellationToken ct)
    {
        return service.SearchAsync(filter, ct);
    }
}
