using EnterpriseAgentOs.Domain.Features.Agents.Integrations;

namespace EnterpriseAgentOs.Api.Features.Agents.Integrations;

[ExtendObjectType(typeof(GraphQLQueries))]
public sealed class IntegrationQueries
{
    public Task<IReadOnlyList<IntegrationDefinitionRecord>> GetIntegrationDefinitions(
        [Service] IIntegrationConnectionService service,
        CancellationToken ct)
    {
        return service.ListIntegrationDefinitionsAsync(ct);
    }

    public Task<IReadOnlyList<IntegrationConnectionRecord>> GetIntegrationConnections(
        IntegrationConnectionFilter? filter,
        [Service] IIntegrationConnectionService service,
        CancellationToken ct)
    {
        return service.ListAsync(filter ?? new IntegrationConnectionFilter(), ct);
    }

    public Task<IntegrationConnectionRecord?> GetIntegrationConnection(
        IntegrationConnectionFilter filter,
        [Service] IIntegrationConnectionService service,
        CancellationToken ct)
    {
        return service.GetByAsync(filter, ct);
    }

    public Task<IReadOnlyList<IntegrationRequestHistoryRecord>> GetIntegrationRequestHistory(
        IntegrationRequestHistoryFilter? filter,
        [Service] IIntegrationConnectionService service,
        CancellationToken ct)
    {
        return service.ListAsync(filter ?? new IntegrationRequestHistoryFilter(), ct);
    }

    public Task<IReadOnlyList<IntegrationActivityRecord>> GetIntegrationActivity(
        IntegrationActivityFilter? filter,
        [Service] IIntegrationConnectionService service,
        CancellationToken ct)
    {
        return service.ListAsync(filter ?? new IntegrationActivityFilter(), ct);
    }

    public Task<IReadOnlyList<IntegrationIndexJobRecord>> GetIntegrationIndexJobs(
        IntegrationIndexJobFilter filter,
        [Service] IIntegrationConnectionService service,
        CancellationToken ct)
    {
        return service.ListAsync(filter, ct);
    }

    public Task<IntegrationIndexedRecordRecord?> GetIntegrationIndexedRecord(
        IntegrationIndexedRecordFilter filter,
        [Service] IIntegrationConnectionService service,
        CancellationToken ct)
    {
        return service.GetByAsync(filter, ct);
    }

    public Task<IntegrationIndexedRecordPage> GetIntegrationIndexedRecords(
        IntegrationIndexedRecordFilter filter,
        [Service] IIntegrationConnectionService service,
        CancellationToken ct)
    {
        return service.SearchAsync(filter, ct);
    }
}
