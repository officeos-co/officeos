namespace OffceOs.Api.Features.Management;

[ExtendObjectType(typeof(GraphQLQueries))]
public sealed class WorkspaceQueries
{
    public async Task<IReadOnlyList<WorkspacePayload>> GetWorkspaces(
        [Service] UserContext user,
        [Service] IWorkspaceService workspaces,
        CancellationToken ct)
    {
        var rows = await workspaces.ListAsync(user.Id, ct);
        return rows.Select(WorkspaceGraphQLMapper.ToPayload).ToList();
    }

    public async Task<WorkspacePayload> GetCurrentWorkspace(
        [Service] UserContext user,
        [Service] IWorkspaceService workspaces,
        CancellationToken ct)
    {
        var row = await workspaces.GetCurrentAsync(user.Id, ct);
        return WorkspaceGraphQLMapper.ToPayload(row);
    }
}
