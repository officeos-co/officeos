namespace EnterpriseAgentOs.Api.Features.Agents;

[ExtendObjectType(typeof(GraphQLQueries))]
public class AgentResourceQueries
{
    public async Task<IReadOnlyList<BrowserResourcePayload>> GetBrowserResources(
        [Service] UserContext user,
        [Service] IAgentResourceRepository resources,
        CancellationToken ct)
    {
        var rows = await resources.ListBrowserResourcesAsync(user.Id, ct);
        return rows.Select(ToPayload).ToList();
    }

    public async Task<BrowserResourcePayload?> GetBrowserResource(
        Guid id,
        [Service] UserContext user,
        [Service] IAgentResourceRepository resources,
        CancellationToken ct)
    {
        var row = await resources.GetBrowserResourceAsync(id, user.Id, ct);
        return row is null ? null : ToPayload(row);
    }

    public async Task<IReadOnlyList<AgentSessionResourceAttachmentPayload>> GetAgentSessionResourceAttachments(
        Guid sessionId,
        [Service] IAgentResourceRepository resources,
        CancellationToken ct)
    {
        var rows = await resources.ListSessionAttachmentsAsync(sessionId, ct);
        return rows.Select(ToPayload).ToList();
    }

    private static BrowserResourcePayload ToPayload(BrowserResourceRecord row) =>
        new(row.Id, row.OwnerId, row.DisplayName, row.CurrentAgentId, row.CreatedAt, row.UpdatedAt);

    private static AgentSessionResourceAttachmentPayload ToPayload(AgentSessionResourceAttachmentRecord row) =>
        new(row.Id, row.AgentId, row.SessionId, row.ResourceType, row.ResourceId, row.AccessMode, row.Instructions, row.CreatedAt);
}
