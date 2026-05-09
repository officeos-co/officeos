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

    public async Task<WorkspacePayload> CreateOrganizationWorkspace(
        CreateOrganizationWorkspaceInput input,
        [Service] UserContext user,
        [Service] IWorkspaceService workspaces,
        CancellationToken ct)
    {
        try
        {
            var created = await workspaces.CreateOrganizationWorkspaceAsync(user.Id, input.OrganizationId, input.Name, ct);
            return WorkspaceGraphQLMapper.ToPayload(created);
        }
        catch (InvalidOperationException ex)
        {
            throw NotFound(ex.Message);
        }
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

    public async Task<WorkspaceMemberPayload> AddWorkspaceMember(
        AddWorkspaceMemberInput input,
        [Service] UserContext user,
        [Service] IWorkspaceService workspaces,
        CancellationToken ct)
    {
        try
        {
            var member = await workspaces.AddMemberAsync(user.Id, input.WorkspaceId, input.UserId, input.Role, ct);
            return WorkspaceGraphQLMapper.ToPayload(member);
        }
        catch (InvalidOperationException ex)
        {
            throw NotFound(ex.Message);
        }
    }

    public async Task<WorkspaceMemberPayload> UpdateWorkspaceMemberRole(
        UpdateWorkspaceMemberRoleInput input,
        [Service] UserContext user,
        [Service] IWorkspaceService workspaces,
        CancellationToken ct)
    {
        try
        {
            var member = await workspaces.UpdateMemberRoleAsync(user.Id, input.WorkspaceId, input.UserId, input.Role, ct);
            return WorkspaceGraphQLMapper.ToPayload(member);
        }
        catch (InvalidOperationException ex)
        {
            throw NotFound(ex.Message);
        }
    }

    public async Task<bool> RemoveWorkspaceMember(
        Guid workspaceId,
        Guid userId,
        [Service] UserContext user,
        [Service] IWorkspaceService workspaces,
        CancellationToken ct)
    {
        try
        {
            return await workspaces.RemoveMemberAsync(user.Id, workspaceId, userId, ct);
        }
        catch (InvalidOperationException ex)
        {
            throw NotFound(ex.Message);
        }
    }

    public async Task<WorkspaceOrganizationGrantPayload> GrantWorkspaceToOrganization(
        GrantWorkspaceOrganizationInput input,
        [Service] UserContext user,
        [Service] IWorkspaceService workspaces,
        CancellationToken ct)
    {
        try
        {
            var grant = await workspaces.GrantOrganizationAsync(
                user.Id,
                input.WorkspaceId,
                input.OrganizationId,
                input.MaxRole,
                ct);
            return WorkspaceGraphQLMapper.ToPayload(grant);
        }
        catch (InvalidOperationException ex)
        {
            throw NotFound(ex.Message);
        }
    }

    public async Task<bool> RevokeWorkspaceOrganizationGrant(
        Guid workspaceId,
        Guid organizationId,
        [Service] UserContext user,
        [Service] IWorkspaceService workspaces,
        CancellationToken ct)
    {
        try
        {
            return await workspaces.RevokeOrganizationGrantAsync(user.Id, workspaceId, organizationId, ct);
        }
        catch (InvalidOperationException ex)
        {
            throw NotFound(ex.Message);
        }
    }

    private static GraphQLException NotFound(string message) =>
        new(ErrorBuilder.New().SetMessage(message).SetCode("NOT_FOUND").Build());
}
