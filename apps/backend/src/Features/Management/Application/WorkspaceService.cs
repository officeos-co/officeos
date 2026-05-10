namespace OffceOs.Application.Features.Management;

internal sealed class WorkspaceService : IWorkspaceService
{
    private readonly IWorkspaceRepository _workspaceRepository;
    private readonly IWorkspaceMemberRepository _workspaceMemberRepository;
    private readonly IOrganizationRepository _organizationRepository;
    private readonly IDistributedCache _distributedCache;
    private readonly IPublisher _publisher;

    public WorkspaceService(
        IWorkspaceRepository workspaceRepository,
        IWorkspaceMemberRepository workspaceMemberRepository,
        IOrganizationRepository organizationRepository,
        IDistributedCache distributedCache,
        IPublisher publisher)
    {
        _workspaceRepository = workspaceRepository;
        _workspaceMemberRepository = workspaceMemberRepository;
        _organizationRepository = organizationRepository;
        _distributedCache = distributedCache;
        _publisher = publisher;
    }

    public async Task<IReadOnlyList<WorkspaceRecord>> ListAsync(Guid userId, CancellationToken ct = default)
    {
        await _workspaceRepository.EnsurePersonalDefaultAsync(userId, ct);
        return await _workspaceRepository.ListAccessibleAsync(userId, ct);
    }

    public Task<WorkspaceRecord> GetCurrentAsync(Guid userId, CancellationToken ct = default)
        => _workspaceRepository.GetCurrentAsync(userId, ct);

    public async Task<WorkspaceRecord> CreateAsync(Guid userId, string? name, CancellationToken ct = default)
    {
        var created = await _workspaceRepository.SaveAsync(WorkspaceRecord.CreatePersonal(userId, name), ct);
        await _workspaceMemberRepository.UpsertAsync(
            WorkspaceMemberRecord.Create(created.Id, userId, WorkspaceRole.Owner),
            ct);
        await InvalidateUserAsync(userId, ct);
        return created;
    }

    public async Task<WorkspaceRecord> CreateOrganizationWorkspaceAsync(Guid userId, Guid organizationId, string? name, CancellationToken ct = default)
    {
        await RequireOrganizationAdminAsync(userId, organizationId, ct);

        var created = await _workspaceRepository.SaveAsync(WorkspaceRecord.CreateOrganization(organizationId, name), ct);
        await _workspaceMemberRepository.UpsertAsync(
            WorkspaceMemberRecord.Create(created.Id, userId, WorkspaceRole.Owner),
            ct);
        await InvalidateUserAsync(userId, ct);
        await _publisher.Publish(new OrganizationWorkspaceCreatedEvent(
            organizationId,
            userId,
            created.Id,
            created.Name), ct);
        return created;
    }

    public async Task<WorkspaceRecord> UpdateAsync(Guid userId, Guid id, string? name, CancellationToken ct = default)
    {
        var workspace = await RequireAccessibleAsync(userId, id, ct);
        if (!CanAdministerWorkspace(workspace, userId))
        {
            var membership = await _workspaceMemberRepository.GetByAsync(new WorkspaceMemberFilter { WorkspaceId = id, UserId = userId }, ct);
            if (membership?.Role.CanAdminister() != true)
                throw new InvalidOperationException("Workspace not found.");
        }

        workspace = await _workspaceRepository.GetByAsync(new WorkspaceFilter { Id = id }, ct)
            ?? throw new InvalidOperationException("Workspace not found.");

        var previousName = workspace.Name;
        workspace.Rename(name);
        var updated = await _workspaceRepository.SaveAsync(workspace, ct);
        await InvalidateUserAsync(userId, ct);
        if (updated.OrganizationId.HasValue)
        {
            await _publisher.Publish(new WorkspaceUpdatedEvent(
                updated.OrganizationId.Value,
                userId,
                updated.Id,
                previousName,
                updated.Name), ct);
        }

        return updated;
    }

    public async Task<WorkspaceRecord> SwitchAsync(Guid userId, Guid id, CancellationToken ct = default)
    {
        var workspace = await RequireAccessibleAsync(userId, id, ct);

        await _workspaceRepository.SetCurrentAsync(userId, id, ct);
        await InvalidateUserAsync(userId, ct);
        return workspace;
    }

    public async Task<bool> DeleteAsync(Guid userId, Guid id, CancellationToken ct = default)
    {
        var workspace = await RequireAccessibleAsync(userId, id, ct);
        if (workspace.IsDefault)
            throw new InvalidOperationException("Default workspaces cannot be deleted.");

        if (!CanAdministerWorkspace(workspace, userId))
        {
            var membership = await _workspaceMemberRepository.GetByAsync(new WorkspaceMemberFilter { WorkspaceId = id, UserId = userId }, ct);
            if (membership?.Role.CanAdminister() != true)
                throw new InvalidOperationException("Workspace not found.");
        }

        var deleted = await _workspaceRepository.DeleteAsync(id, ct);
        if (deleted)
        {
            await _workspaceRepository.GetCurrentAsync(userId, ct);
            await InvalidateUserAsync(userId, ct);
            if (workspace.OrganizationId.HasValue)
            {
                await _publisher.Publish(new WorkspaceDeletedEvent(
                    workspace.OrganizationId.Value,
                    userId,
                    workspace.Id,
                    workspace.Name), ct);
            }
        }

        return deleted;
    }

    public async Task<WorkspaceRecord> RequireAccessibleAsync(Guid userId, Guid workspaceId, CancellationToken ct = default)
    {
        return await _workspaceRepository.GetAccessibleAsync(userId, workspaceId, ct)
            ?? throw new InvalidOperationException("Workspace not found.");
    }

    public async Task<WorkspaceMemberRecord> AddMemberAsync(
        Guid actorUserId,
        Guid workspaceId,
        Guid memberUserId,
        string? role,
        CancellationToken ct = default)
    {
        await RequireWorkspaceAdminAsync(actorUserId, workspaceId, ct);
        var member = await _workspaceMemberRepository.UpsertAsync(
            WorkspaceMemberRecord.Create(workspaceId, memberUserId, ParseWorkspaceRole(role, WorkspaceRole.Editor)),
            ct);
        var workspace = await _workspaceRepository.GetByAsync(new WorkspaceFilter { Id = workspaceId }, ct);
        if (workspace?.OrganizationId.HasValue == true)
        {
            await _publisher.Publish(new WorkspaceMemberAddedEvent(
                workspace.OrganizationId.Value,
                actorUserId,
                workspaceId,
                memberUserId,
                member.Role.ToString()), ct);
        }

        return member;
    }

    public async Task<WorkspaceMemberRecord> UpdateMemberRoleAsync(
        Guid actorUserId,
        Guid workspaceId,
        Guid memberUserId,
        string? role,
        CancellationToken ct = default)
    {
        await RequireWorkspaceAdminAsync(actorUserId, workspaceId, ct);
        var existing = await _workspaceMemberRepository.GetByAsync(
            new WorkspaceMemberFilter { WorkspaceId = workspaceId, UserId = memberUserId },
            ct) ?? throw new InvalidOperationException("Workspace member not found.");

        var previousRole = existing.Role;
        existing.Role = ParseWorkspaceRole(role, WorkspaceRole.Editor);
        var updated = await _workspaceMemberRepository.UpsertAsync(existing, ct);
        var workspace = await _workspaceRepository.GetByAsync(new WorkspaceFilter { Id = workspaceId }, ct);
        if (workspace?.OrganizationId.HasValue == true)
        {
            await _publisher.Publish(new WorkspaceMemberRoleUpdatedEvent(
                workspace.OrganizationId.Value,
                actorUserId,
                workspaceId,
                memberUserId,
                previousRole.ToString(),
                updated.Role.ToString()), ct);
        }

        return updated;
    }

    public async Task<bool> RemoveMemberAsync(Guid actorUserId, Guid workspaceId, Guid memberUserId, CancellationToken ct = default)
    {
        await RequireWorkspaceAdminAsync(actorUserId, workspaceId, ct);
        if (actorUserId == memberUserId)
            throw new InvalidOperationException("Workspace owners cannot remove themselves.");

        var removed = await _workspaceMemberRepository.DeleteAsync(
            new WorkspaceMemberFilter { WorkspaceId = workspaceId, UserId = memberUserId },
            ct);
        if (removed)
        {
            var workspace = await _workspaceRepository.GetByAsync(new WorkspaceFilter { Id = workspaceId }, ct);
            if (workspace?.OrganizationId.HasValue == true)
            {
                await _publisher.Publish(new WorkspaceMemberRemovedEvent(
                    workspace.OrganizationId.Value,
                    actorUserId,
                    workspaceId,
                    memberUserId), ct);
            }
        }

        return removed;
    }

    public async Task<WorkspaceOrganizationGrantRecord> GrantOrganizationAsync(
        Guid actorUserId,
        Guid workspaceId,
        Guid organizationId,
        string? maxRole,
        CancellationToken ct = default)
    {
        var workspace = await RequireWorkspaceAdminAsync(actorUserId, workspaceId, ct);
        if (workspace.OrganizationId == organizationId)
            throw new InvalidOperationException("Cannot grant a workspace to its owning organization.");

        var organization = await _organizationRepository.GetByAsync(new OrganizationFilter { Id = organizationId }, ct)
            ?? throw new InvalidOperationException("Organization not found.");

        _ = organization;
        var grant = await _workspaceRepository.UpsertOrganizationGrantAsync(new WorkspaceOrganizationGrantRecord
        {
            WorkspaceId = workspaceId,
            OrganizationId = organizationId,
            MaxRole = ParseWorkspaceRole(maxRole, WorkspaceRole.Viewer),
        }, ct);
        if (workspace.OrganizationId.HasValue)
        {
            await _publisher.Publish(new WorkspaceOrganizationGrantCreatedEvent(
                workspace.OrganizationId.Value,
                actorUserId,
                workspaceId,
                organizationId,
                grant.MaxRole.ToString()), ct);
        }

        return grant;
    }

    public async Task<bool> RevokeOrganizationGrantAsync(Guid actorUserId, Guid workspaceId, Guid organizationId, CancellationToken ct = default)
    {
        var workspace = await RequireWorkspaceAdminAsync(actorUserId, workspaceId, ct);
        var revoked = await _workspaceRepository.DeleteOrganizationGrantAsync(workspaceId, organizationId, ct);
        if (revoked && workspace.OrganizationId.HasValue)
        {
            await _publisher.Publish(new WorkspaceOrganizationGrantRevokedEvent(
                workspace.OrganizationId.Value,
                actorUserId,
                workspaceId,
                organizationId), ct);
        }

        return revoked;
    }

    private async Task RequireOrganizationAdminAsync(Guid userId, Guid organizationId, CancellationToken ct)
    {
        var members = await _organizationRepository.ListMembersAsync(organizationId, ct);
        var member = members.FirstOrDefault(m => m.UserId == userId && m.Status == MemberStatus.Active);
        if (member?.Role is not (OrgRole.Owner or OrgRole.Admin))
            throw new InvalidOperationException("Workspace not found.");
    }

    private async Task<WorkspaceRecord> RequireWorkspaceAdminAsync(Guid userId, Guid workspaceId, CancellationToken ct)
    {
        var workspace = await RequireAccessibleAsync(userId, workspaceId, ct);
        if (CanAdministerWorkspace(workspace, userId))
            return workspace;

        var membership = await _workspaceMemberRepository.GetByAsync(
            new WorkspaceMemberFilter { WorkspaceId = workspaceId, UserId = userId },
            ct);
        if (membership?.Role.CanAdminister() == true)
            return workspace;

        if (workspace.OrganizationId.HasValue)
        {
            var members = await _organizationRepository.ListMembersAsync(workspace.OrganizationId.Value, ct);
            var member = members.FirstOrDefault(m => m.UserId == userId && m.Status == MemberStatus.Active);
            if (member?.Role is OrgRole.Owner or OrgRole.Admin)
                return workspace;
        }

        throw new InvalidOperationException("Workspace not found.");
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

    private static bool CanAdministerWorkspace(WorkspaceRecord workspace, Guid userId)
    {
        return workspace.OwnerKind == WorkspaceOwnerKind.Personal && workspace.OwnerUserId == userId;
    }

    private async Task InvalidateUserAsync(Guid userId, CancellationToken ct)
    {
        await _distributedCache.RemoveAsync($"auth:me:{userId}", ct);
    }
}
