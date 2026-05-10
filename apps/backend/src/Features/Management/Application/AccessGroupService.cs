namespace OffceOs.Application.Features.Management;

internal sealed class AccessGroupService : IAccessGroupService
{
    private readonly IAccessGroupRepository _accessGroupRepository;
    private readonly IOrganizationRepository _organizationRepository;
    private readonly IWorkspaceRepository _workspaceRepository;

    public AccessGroupService(
        IAccessGroupRepository accessGroupRepository,
        IOrganizationRepository organizationRepository,
        IWorkspaceRepository workspaceRepository)
    {
        _accessGroupRepository = accessGroupRepository;
        _organizationRepository = organizationRepository;
        _workspaceRepository = workspaceRepository;
    }

    public async Task<IReadOnlyList<AccessGroupRecord>> ListAsync(Guid actorUserId, Guid organizationId, CancellationToken ct = default)
    {
        await RequireOrganizationAdminAsync(actorUserId, organizationId, ct);
        return await _accessGroupRepository.ListAsync(new AccessGroupFilter { OrganizationId = organizationId }, ct);
    }

    public async Task<AccessGroupRecord> CreateAsync(Guid actorUserId, Guid organizationId, string name, CancellationToken ct = default)
    {
        await RequireOrganizationAdminAsync(actorUserId, organizationId, ct);
        return await _accessGroupRepository.SaveAsync(AccessGroupRecord.Create(organizationId, name), ct);
    }

    public async Task<AccessGroupRecord> RenameAsync(Guid actorUserId, Guid accessGroupId, string name, CancellationToken ct = default)
    {
        var group = await RequireGroupAdminAsync(actorUserId, accessGroupId, ct);
        group.Rename(name);
        return await _accessGroupRepository.SaveAsync(group, ct);
    }

    public async Task<bool> DeleteAsync(Guid actorUserId, Guid accessGroupId, CancellationToken ct = default)
    {
        await RequireGroupAdminAsync(actorUserId, accessGroupId, ct);
        return await _accessGroupRepository.DeleteAsync(new AccessGroupFilter { Id = accessGroupId }, ct);
    }

    public async Task<AccessGroupMemberRecord> AddMemberAsync(Guid actorUserId, Guid accessGroupId, Guid userId, CancellationToken ct = default)
    {
        await RequireGroupAdminAsync(actorUserId, accessGroupId, ct);
        return await _accessGroupRepository.AddMemberAsync(accessGroupId, userId, ct);
    }

    public async Task<bool> RemoveMemberAsync(Guid actorUserId, Guid accessGroupId, Guid userId, CancellationToken ct = default)
    {
        await RequireGroupAdminAsync(actorUserId, accessGroupId, ct);
        return await _accessGroupRepository.RemoveMemberAsync(accessGroupId, userId, ct);
    }

    public async Task<AccessGroupWorkspaceGrantRecord> GrantWorkspaceAsync(Guid actorUserId, Guid accessGroupId, Guid workspaceId, string? role, CancellationToken ct = default)
    {
        var group = await RequireGroupAdminAsync(actorUserId, accessGroupId, ct);
        var workspace = await _workspaceRepository.GetByAsync(new WorkspaceFilter { Id = workspaceId }, ct)
            ?? throw new InvalidOperationException("Workspace not found.");
        if (workspace.OrganizationId != group.OrganizationId)
            throw new InvalidOperationException("Access groups can only grant access to workspaces in the same organization.");

        return await _accessGroupRepository.UpsertWorkspaceGrantAsync(new AccessGroupWorkspaceGrantRecord
        {
            AccessGroupId = accessGroupId,
            WorkspaceId = workspaceId,
            Role = ParseWorkspaceRole(role, WorkspaceRole.Viewer),
        }, ct);
    }

    public async Task<bool> RevokeWorkspaceAsync(Guid actorUserId, Guid accessGroupId, Guid workspaceId, CancellationToken ct = default)
    {
        await RequireGroupAdminAsync(actorUserId, accessGroupId, ct);
        return await _accessGroupRepository.DeleteWorkspaceGrantAsync(accessGroupId, workspaceId, ct);
    }

    private async Task<AccessGroupRecord> RequireGroupAdminAsync(Guid actorUserId, Guid accessGroupId, CancellationToken ct)
    {
        var group = await _accessGroupRepository.GetByAsync(new AccessGroupFilter { Id = accessGroupId }, ct)
            ?? throw new InvalidOperationException("Access group not found.");
        await RequireOrganizationAdminAsync(actorUserId, group.OrganizationId, ct);
        return group;
    }

    private async Task RequireOrganizationAdminAsync(Guid userId, Guid organizationId, CancellationToken ct)
    {
        var members = await _organizationRepository.ListMembersAsync(organizationId, ct);
        var member = members.FirstOrDefault(m => m.UserId == userId && m.Status == MemberStatus.Active);
        if (member?.Role is not (OrgRole.Owner or OrgRole.Admin))
            throw new InvalidOperationException("Organization not found.");
    }

    private static WorkspaceRole ParseWorkspaceRole(string? value, WorkspaceRole fallback)
    {
        if (string.IsNullOrWhiteSpace(value))
            return fallback;

        return value.Trim() switch
        {
            "Owner" => WorkspaceRole.Owner,
            "Admin" => WorkspaceRole.Admin,
            "Editor" => WorkspaceRole.Editor,
            "Viewer" => WorkspaceRole.Viewer,
            _ => throw new InvalidOperationException("Workspace role must be 'Owner', 'Admin', 'Editor', or 'Viewer'."),
        };
    }
}
