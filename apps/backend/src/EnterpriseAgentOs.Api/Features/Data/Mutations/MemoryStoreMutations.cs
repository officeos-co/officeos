namespace EnterpriseAgentOs.Api.Features.Data;

[ExtendObjectType(typeof(GraphQLMutations))]
public sealed class MemoryStoreMutations
{
    public async Task<MemoryStorePayload> CreateMemoryStore(
        CreateMemoryStoreInput input,
        IResolverContext context,
        [Service] IMemoryStoreRepository memoryStores,
        CancellationToken ct)
    {
        var user = DashboardAuthContextExtensions.GetUser(context);
        var row = await memoryStores.CreateAsync(
            MemoryStoreRecord.Create(user.Id, input.DisplayName ?? "Memory Store"),
            ct);
        return new MemoryStorePayload(row.Id, row.OwnerId, row.DisplayName, row.CreatedAt, row.UpdatedAt, []);
    }

    public async Task<bool> DeleteMemoryStore(
        Guid id,
        IResolverContext context,
        [Service] IMemoryStoreRepository memoryStores,
        CancellationToken ct)
    {
        var user = DashboardAuthContextExtensions.GetUser(context);
        return await memoryStores.DeleteAsync(id, user.Id, ct);
    }

    public async Task<MemoryStoreEntryPayload> UpsertMemoryStoreEntry(
        UpsertMemoryStoreEntryInput input,
        IResolverContext context,
        [Service] IMemoryStoreRepository memoryStores,
        CancellationToken ct)
    {
        var user = DashboardAuthContextExtensions.GetUser(context);
        var row = await memoryStores.UpsertEntryAsync(input.MemoryStoreId, user.Id, input.Key, input.Content, ct);
        return new MemoryStoreEntryPayload(row.Id, row.MemoryStoreId, row.Key, row.Content, row.CreatedAt, row.UpdatedAt);
    }

    public async Task<bool> DeleteMemoryStoreEntry(
        Guid memoryStoreId,
        string key,
        IResolverContext context,
        [Service] IMemoryStoreRepository memoryStores,
        CancellationToken ct)
    {
        var user = DashboardAuthContextExtensions.GetUser(context);
        return await memoryStores.DeleteEntryAsync(memoryStoreId, user.Id, key, ct);
    }
}
