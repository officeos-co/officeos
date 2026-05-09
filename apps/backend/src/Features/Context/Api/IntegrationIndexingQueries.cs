namespace OffceOs.Api.Features.Context;

[ExtendObjectType(typeof(GraphQLQueries))]
public sealed class IntegrationQueries
{
    public Task<IReadOnlyList<IntegrationDefinitionRecord>> GetIntegrationDefinitions(
        [Service] UserContext user,
        [Service] IIntegrationConnectionService service,
        CancellationToken ct)
    {
        return service.ListIntegrationDefinitionsAsync(user.Id, ct);
    }

    public async Task<IReadOnlyList<IntegrationConnectionRecord>> GetIntegrationConnections(
        IntegrationConnectionFilter? filter,
        [Service] UserContext user,
        [Service] IWorkspaceService workspaces,
        [Service] IIntegrationConnectionService service,
        CancellationToken ct)
    {
        var workspace = await workspaces.GetCurrentAsync(user.Id, ct);
        return await service.ListAsync((filter ?? new IntegrationConnectionFilter()) with
        {
            CreatedById = user.Id,
            WorkspaceId = workspace.Id,
        }, ct);
    }

    public async Task<IntegrationConnectionRecord?> GetIntegrationConnection(
        IntegrationConnectionFilter filter,
        [Service] UserContext user,
        [Service] IWorkspaceService workspaces,
        [Service] IIntegrationConnectionService service,
        CancellationToken ct)
    {
        var workspace = await workspaces.GetCurrentAsync(user.Id, ct);
        return await service.GetByAsync(filter with { CreatedById = user.Id, WorkspaceId = workspace.Id }, ct);
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
