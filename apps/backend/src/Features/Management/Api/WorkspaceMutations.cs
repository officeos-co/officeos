namespace OffceOs.Api.Features.Management;

[ExtendObjectType(typeof(GraphQLMutations))]
public sealed class WorkspaceMutations
{
    public async Task<WorkspacePayload> CreateWorkspace(
        CreateWorkspaceInput input,
        [Service] UserContext user,
        [Service] IWorkspaceService workspaces,
        CancellationToken ct)
    {
        var created = await workspaces.CreateAsync(user.Id, input.Name, ct);
        return WorkspaceGraphQLMapper.ToPayload(created);
    }

    public async Task<WorkspacePayload> UpdateWorkspace(
        Guid id,
        UpdateWorkspaceInput input,
        [Service] UserContext user,
        [Service] IWorkspaceService workspaces,
        CancellationToken ct)
    {
        try
        {
            var updated = await workspaces.UpdateAsync(user.Id, id, input.Name, ct);
            return WorkspaceGraphQLMapper.ToPayload(updated);
        }
        catch (InvalidOperationException ex)
        {
            throw NotFound(ex.Message);
        }
    }

    public async Task<WorkspacePayload> SwitchWorkspace(
        Guid id,
        [Service] UserContext user,
        [Service] IWorkspaceService workspaces,
        CancellationToken ct)
    {
        try
        {
            var switched = await workspaces.SwitchAsync(user.Id, id, ct);
            return WorkspaceGraphQLMapper.ToPayload(switched);
        }
        catch (InvalidOperationException ex)
        {
            throw NotFound(ex.Message);
        }
    }

    public async Task<bool> DeleteWorkspace(
        Guid id,
        [Service] UserContext user,
        [Service] IWorkspaceService workspaces,
        CancellationToken ct)
    {
        return await workspaces.DeleteAsync(user.Id, id, ct);
    }

    private static GraphQLException NotFound(string message) =>
        new(ErrorBuilder.New().SetMessage(message).SetCode("NOT_FOUND").Build());
}
