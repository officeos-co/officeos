namespace EnterpriseAgentOs.Api.Features.Data;

[ExtendObjectType(typeof(GraphQLQueries))]
public sealed class MemoryStoreQueries
{
    public async Task<IReadOnlyList<MemoryStorePayload>> GetMemoryStores(
        IResolverContext context,
        [Service] IMemoryStoreRepository memoryStores,
        CancellationToken ct)
    {
        var user = DashboardAuthContextExtensions.GetUser(context);
        var rows = await memoryStores.ListAsync(user.Id, ct);
        return rows.Select(row => ToPayload(row, null)).ToList();
    }

    public async Task<MemoryStorePayload?> GetMemoryStore(
        Guid id,
        IResolverContext context,
        [Service] IMemoryStoreRepository memoryStores,
        CancellationToken ct)
    {
        var user = DashboardAuthContextExtensions.GetUser(context);
        var row = await memoryStores.GetAsync(id, user.Id, ct);
        if (row is null) return null;

        var entries = await memoryStores.ListEntriesAsync(id, user.Id, ct);
        return ToPayload(row, entries);
    }

    private static MemoryStorePayload ToPayload(MemoryStoreRecord row, IReadOnlyList<MemoryStoreEntryRecord>? entries) =>
        new(row.Id, row.OwnerId, row.DisplayName, row.CreatedAt, row.UpdatedAt, entries?.Select(ToPayload).ToList());

    private static MemoryStoreEntryPayload ToPayload(MemoryStoreEntryRecord row) =>
        new(row.Id, row.MemoryStoreId, row.Key, row.Content, row.CreatedAt, row.UpdatedAt);
}
