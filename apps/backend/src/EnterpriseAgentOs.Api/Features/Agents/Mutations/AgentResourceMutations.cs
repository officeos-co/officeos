namespace EnterpriseAgentOs.Api.Features.Agents;

[ExtendObjectType(typeof(GraphQLMutations))]
public class AgentResourceMutations
{
    public async Task<BrowserResourcePayload> CreateBrowserResource(
        CreateBrowserResourceInput input,
        IResolverContext context,
        [Service] IAgentResourceRepository resources,
        CancellationToken ct)
    {
        var user = DashboardAuthContextExtensions.GetUser(context);
        var row = await resources.CreateBrowserResourceAsync(
            BrowserResourceRecord.Create(user.Id, input.DisplayName ?? "Browser"),
            ct);
        return new BrowserResourcePayload(row.Id, row.OwnerId, row.DisplayName, row.CurrentAgentId, row.CreatedAt, row.UpdatedAt);
    }

    public async Task<bool> DeleteBrowserResource(
        Guid id,
        IResolverContext context,
        [Service] IAgentResourceRepository resources,
        CancellationToken ct)
    {
        var user = DashboardAuthContextExtensions.GetUser(context);
        return await resources.DeleteBrowserResourceAsync(id, user.Id, ct);
    }

    public async Task<MemoryStorePayload> CreateMemoryStore(
        CreateMemoryStoreInput input,
        IResolverContext context,
        [Service] IAgentResourceRepository resources,
        CancellationToken ct)
    {
        var user = DashboardAuthContextExtensions.GetUser(context);
        var row = await resources.CreateMemoryStoreAsync(
            MemoryStoreRecord.Create(user.Id, input.DisplayName ?? "Memory Store"),
            ct);
        return new MemoryStorePayload(row.Id, row.OwnerId, row.DisplayName, row.CreatedAt, row.UpdatedAt, []);
    }

    public async Task<bool> DeleteMemoryStore(
        Guid id,
        IResolverContext context,
        [Service] IAgentResourceRepository resources,
        CancellationToken ct)
    {
        var user = DashboardAuthContextExtensions.GetUser(context);
        return await resources.DeleteMemoryStoreAsync(id, user.Id, ct);
    }

    public async Task<MemoryStoreEntryPayload> UpsertMemoryStoreEntry(
        UpsertMemoryStoreEntryInput input,
        IResolverContext context,
        [Service] IAgentResourceRepository resources,
        CancellationToken ct)
    {
        var user = DashboardAuthContextExtensions.GetUser(context);
        var row = await resources.UpsertMemoryStoreEntryAsync(input.MemoryStoreId, user.Id, input.Key, input.Content, ct);
        return new MemoryStoreEntryPayload(row.Id, row.MemoryStoreId, row.Key, row.Content, row.CreatedAt, row.UpdatedAt);
    }

    public async Task<bool> DeleteMemoryStoreEntry(
        Guid memoryStoreId,
        string key,
        IResolverContext context,
        [Service] IAgentResourceRepository resources,
        CancellationToken ct)
    {
        var user = DashboardAuthContextExtensions.GetUser(context);
        return await resources.DeleteMemoryStoreEntryAsync(memoryStoreId, user.Id, key, ct);
    }
}
