namespace OffceOs.Api.Features.Management;

[ExtendObjectType(typeof(GraphQLMutations))]
public sealed class AccessGroupMutations
{
    public async Task<AccessGroupPayload> CreateAccessGroup(
        CreateAccessGroupInput input,
        [Service] UserContext user,
        [Service] IAccessGroupService accessGroups,
        CancellationToken ct)
    {
        try
        {
            var group = await accessGroups.CreateAsync(user.Id, input.OrganizationId, input.Name, ct);
            return AccessGroupGraphQLMapper.ToPayload(group);
        }
        catch (InvalidOperationException ex)
        {
            throw BadInput(ex.Message);
        }
    }

    public async Task<AccessGroupPayload> RenameAccessGroup(
        RenameAccessGroupInput input,
        [Service] UserContext user,
        [Service] IAccessGroupService accessGroups,
        CancellationToken ct)
    {
        try
        {
            var group = await accessGroups.RenameAsync(user.Id, input.AccessGroupId, input.Name, ct);
            return AccessGroupGraphQLMapper.ToPayload(group);
        }
        catch (InvalidOperationException ex)
        {
            throw BadInput(ex.Message);
        }
    }

    public async Task<bool> DeleteAccessGroup(
        Guid accessGroupId,
        [Service] UserContext user,
        [Service] IAccessGroupService accessGroups,
        CancellationToken ct)
    {
        return await accessGroups.DeleteAsync(user.Id, accessGroupId, ct);
    }

    public async Task<AccessGroupMemberPayload> AddAccessGroupMember(
        AddAccessGroupMemberInput input,
        [Service] UserContext user,
        [Service] IAccessGroupService accessGroups,
        CancellationToken ct)
    {
        try
        {
            var member = await accessGroups.AddMemberAsync(user.Id, input.AccessGroupId, input.UserId, ct);
            return AccessGroupGraphQLMapper.ToPayload(member);
        }
        catch (InvalidOperationException ex)
        {
            throw BadInput(ex.Message);
        }
    }

    public async Task<bool> RemoveAccessGroupMember(
        Guid accessGroupId,
        Guid userId,
        [Service] UserContext user,
        [Service] IAccessGroupService accessGroups,
        CancellationToken ct)
    {
        return await accessGroups.RemoveMemberAsync(user.Id, accessGroupId, userId, ct);
    }

    public async Task<AccessGroupWorkspaceGrantPayload> GrantAccessGroupWorkspace(
        GrantAccessGroupWorkspaceInput input,
        [Service] UserContext user,
        [Service] IAccessGroupService accessGroups,
        CancellationToken ct)
    {
        try
        {
            var grant = await accessGroups.GrantWorkspaceAsync(user.Id, input.AccessGroupId, input.WorkspaceId, input.Role, ct);
            return AccessGroupGraphQLMapper.ToPayload(grant);
        }
        catch (InvalidOperationException ex)
        {
            throw BadInput(ex.Message);
        }
    }

    public async Task<bool> RevokeAccessGroupWorkspace(
        Guid accessGroupId,
        Guid workspaceId,
        [Service] UserContext user,
        [Service] IAccessGroupService accessGroups,
        CancellationToken ct)
    {
        return await accessGroups.RevokeWorkspaceAsync(user.Id, accessGroupId, workspaceId, ct);
    }

    private static GraphQLException BadInput(string message) =>
        new(ErrorBuilder.New().SetMessage(message).SetCode("BAD_INPUT").Build());
}
