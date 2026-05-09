namespace OffceOs.Api.Features.Agents;

[ExtendObjectType(typeof(GraphQLMutations))]
public class AgentResourceMutations
{
    public async Task<BrowserResourcePayload> CreateBrowserResource(
        CreateBrowserResourceInput input,
        [Service] UserContext user,
        [Service] IWorkspaceService workspaces,
        [Service] IAgentResourceService resources,
        CancellationToken ct)
    {
        var workspace = await workspaces.GetCurrentAsync(user.Id, ct);
        var row = await resources.CreateBrowserResourceAsync(user.Id, workspace.Id, input.DisplayName, ct);
        return new BrowserResourcePayload(row.Id, row.OwnerId, row.DisplayName, row.CurrentAgentId, row.CreatedAt, row.UpdatedAt);
    }

    public async Task<bool> DeleteBrowserResource(
        Guid id,
        [Service] UserContext user,
        [Service] IWorkspaceService workspaces,
        [Service] IAgentResourceService resources,
        CancellationToken ct)
    {
        var workspace = await workspaces.GetCurrentAsync(user.Id, ct);
        return await resources.DeleteBrowserResourceAsync(id, user.Id, workspace.Id, ct);
    }

}
