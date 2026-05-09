namespace OffceOs.Api.Features.Context;

[ExtendObjectType(typeof(GraphQLMutations))]
public sealed class MemoryStoreMutations
{
    public async Task<MemoryStorePayload> CreateMemoryStore(
        CreateMemoryStoreInput input,
        [Service] UserContext user,
        [Service] IWorkspaceService workspaces,
        [Service] IMemoryStoreService memoryStores,
        CancellationToken ct)
    {
        var workspace = await workspaces.GetCurrentAsync(user.Id, ct);
        var row = await memoryStores.CreateAsync(user.Id, workspace.Id, input.DisplayName, ct);
        return new MemoryStorePayload(row.Id, row.OwnerId, row.DisplayName, row.CreatedAt, row.UpdatedAt, []);
    }

    public async Task<bool> DeleteMemoryStore(
        Guid id,
        [Service] UserContext user,
        [Service] IWorkspaceService workspaces,
        [Service] IMemoryStoreService memoryStores,
        CancellationToken ct)
    {
        var workspace = await workspaces.GetCurrentAsync(user.Id, ct);
        return await memoryStores.DeleteAsync(id, user.Id, workspace.Id, ct);
    }

    public async Task<MemoryStoreEntryPayload> UpsertMemoryStoreEntry(
        UpsertMemoryStoreEntryInput input,
        [Service] UserContext user,
        [Service] IWorkspaceService workspaces,
        [Service] IMemoryStoreService memoryStores,
        CancellationToken ct)
    {
        var workspace = await workspaces.GetCurrentAsync(user.Id, ct);
        var row = await memoryStores.UpsertEntryAsync(input.MemoryStoreId, user.Id, workspace.Id, input.Key, input.Content, ct);
        return new MemoryStoreEntryPayload(row.Id, row.MemoryStoreId, row.Key, row.Content, row.CreatedAt, row.UpdatedAt);
    }

    public async Task<bool> DeleteMemoryStoreEntry(
        Guid memoryStoreId,
        string key,
        [Service] UserContext user,
        [Service] IWorkspaceService workspaces,
        [Service] IMemoryStoreService memoryStores,
        CancellationToken ct)
    {
        var workspace = await workspaces.GetCurrentAsync(user.Id, ct);
        return await memoryStores.DeleteEntryAsync(memoryStoreId, user.Id, workspace.Id, key, ct);
    }
}
