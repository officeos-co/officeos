namespace EnterpriseAgentOs.Api.Features.Agents;

[ExtendObjectType(typeof(GraphQLQueries))]
public class AgentResourceQueries
{
    public async Task<IReadOnlyList<BrowserResourcePayload>> GetBrowserResources(
        IResolverContext context,
        [Service] IAgentResourceRepository resources,
        CancellationToken ct)
    {
        var user = DashboardAuthContextExtensions.GetUser(context);
        var rows = await resources.ListBrowserResourcesAsync(user.Id, ct);
        return rows.Select(ToPayload).ToList();
    }

    public async Task<BrowserResourcePayload?> GetBrowserResource(
        Guid id,
        IResolverContext context,
        [Service] IAgentResourceRepository resources,
        CancellationToken ct)
    {
        var user = DashboardAuthContextExtensions.GetUser(context);
        var row = await resources.GetBrowserResourceAsync(id, user.Id, ct);
        return row is null ? null : ToPayload(row);
    }

    public async Task<IReadOnlyList<MemoryStorePayload>> GetMemoryStores(
        IResolverContext context,
        [Service] IAgentResourceRepository resources,
        CancellationToken ct)
    {
        var user = DashboardAuthContextExtensions.GetUser(context);
        var rows = await resources.ListMemoryStoresAsync(user.Id, ct);
        return rows.Select(row => ToPayload(row, null)).ToList();
    }

    public async Task<MemoryStorePayload?> GetMemoryStore(
        Guid id,
        IResolverContext context,
        [Service] IAgentResourceRepository resources,
        CancellationToken ct)
    {
        var user = DashboardAuthContextExtensions.GetUser(context);
        var row = await resources.GetMemoryStoreAsync(id, user.Id, ct);
        if (row is null) return null;
        var entries = await resources.ListMemoryStoreEntriesAsync(id, user.Id, ct);
        return ToPayload(row, entries);
    }

    public async Task<IReadOnlyList<AgentSessionResourceAttachmentPayload>> GetAgentSessionResourceAttachments(
        Guid sessionId,
        IResolverContext context,
        [Service] IAgentResourceRepository resources,
        CancellationToken ct)
    {
        _ = DashboardAuthContextExtensions.GetUser(context);
        var rows = await resources.ListSessionAttachmentsAsync(sessionId, ct);
        return rows.Select(ToPayload).ToList();
    }

    private static BrowserResourcePayload ToPayload(BrowserResourceRecord row) =>
        new(row.Id, row.OwnerId, row.DisplayName, row.CurrentAgentId, row.CreatedAt, row.UpdatedAt);

    private static MemoryStorePayload ToPayload(MemoryStoreRecord row, IReadOnlyList<MemoryStoreEntryRecord>? entries) =>
        new(row.Id, row.OwnerId, row.DisplayName, row.CreatedAt, row.UpdatedAt, entries?.Select(ToPayload).ToList());

    private static MemoryStoreEntryPayload ToPayload(MemoryStoreEntryRecord row) =>
        new(row.Id, row.MemoryStoreId, row.Key, row.Content, row.CreatedAt, row.UpdatedAt);

    private static AgentSessionResourceAttachmentPayload ToPayload(AgentSessionResourceAttachmentRecord row) =>
        new(row.Id, row.AgentId, row.SessionId, row.ResourceType, row.ResourceId, row.AccessMode, row.Instructions, row.CreatedAt);
}
