using EnterpriseAgentOs.Domain.Features.Agents.Integrations;

namespace EnterpriseAgentOs.Api.Features.Agents.Integrations;

[ExtendObjectType(typeof(GraphQLQueries))]
public sealed class AtlasQueries
{
    public Task<IReadOnlyList<IntegrationDefinitionRecord>> GetAtlasConnectorTypes(
        [Service] IIntegrationConnectionService service,
        CancellationToken ct)
    {
        return service.ListConnectorTypesAsync(ct);
    }

    public Task<IReadOnlyList<IntegrationConnectionRecord>> GetAtlasConnections(
        IntegrationConnectionFilter? filter,
        [Service] IIntegrationConnectionService service,
        CancellationToken ct)
    {
        return service.ListAsync(filter ?? new IntegrationConnectionFilter(), ct);
    }

    public Task<IntegrationConnectionRecord?> GetAtlasConnection(
        IntegrationConnectionFilter filter,
        [Service] IIntegrationConnectionService service,
        CancellationToken ct)
    {
        return service.GetByAsync(filter, ct);
    }

    public Task<IReadOnlyList<IntegrationRequestHistoryRecord>> GetAtlasRequestHistory(
        IntegrationRequestHistoryFilter? filter,
        [Service] IIntegrationConnectionService service,
        CancellationToken ct)
    {
        return service.ListAsync(filter ?? new IntegrationRequestHistoryFilter(), ct);
    }

    public Task<IReadOnlyList<IntegrationActivityRecord>> GetAtlasActivity(
        IntegrationActivityFilter? filter,
        [Service] IIntegrationConnectionService service,
        CancellationToken ct)
    {
        return service.ListAsync(filter ?? new IntegrationActivityFilter(), ct);
    }

    public Task<IReadOnlyList<IntegrationIndexJobRecord>> GetAtlasIndexJobs(
        IntegrationIndexJobFilter filter,
        [Service] IIntegrationConnectionService service,
        CancellationToken ct)
    {
        return service.ListAsync(filter, ct);
    }

    public Task<IntegrationIndexedRecordRecord?> GetAtlasIndexedRecord(
        IntegrationIndexedRecordFilter filter,
        [Service] IIntegrationConnectionService service,
        CancellationToken ct)
    {
        return service.GetByAsync(filter, ct);
    }

    public Task<IntegrationIndexedRecordPage> GetAtlasIndexedRecords(
        IntegrationIndexedRecordFilter filter,
        [Service] IIntegrationConnectionService service,
        CancellationToken ct)
    {
        return service.SearchAsync(filter, ct);
    }
}
