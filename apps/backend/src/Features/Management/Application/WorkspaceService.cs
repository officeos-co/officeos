using OffceOs.Application.Features.ResourceLogs;
using OffceOs.Domain.Common.ValueObjects;
using OffceOs.Domain.Features.Management;
using OffceOs.Domain.Features.ResourceLogs;
namespace OffceOs.Application.Features.Management;

internal sealed class WorkspaceService : IWorkspaceService
{
    private readonly IWorkspaceRepository _workspaceRepository;
    private readonly IWorkspaceMemberRepository _workspaceMemberRepository;
    private readonly IResourceLogService _resourceLogService;
    private readonly IDistributedCache _distributedCache;

    public WorkspaceService(
        IWorkspaceRepository workspaceRepository,
        IWorkspaceMemberRepository workspaceMemberRepository,
        IResourceLogService resourceLogService,
        IDistributedCache distributedCache)
    {
        _workspaceRepository = workspaceRepository;
        _workspaceMemberRepository = workspaceMemberRepository;
        _resourceLogService = resourceLogService;
        _distributedCache = distributedCache;
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
        await AppendWorkspaceLogAsync(created, ResourceLogType.System, "Workspace created.", ct);
        await InvalidateUserAsync(userId, ct);
        return created;
    }

    public async Task<WorkspaceRecord> UpdateAsync(Guid userId, Guid id, string? name, CancellationToken ct = default)
    {
        await RequireWorkspaceAdminAsync(userId, id, ct);
        var workspace = await _workspaceRepository.GetByAsync(new WorkspaceFilter { Id = id }, ct)
            ?? throw new InvalidOperationException("Workspace not found.");

        workspace.Rename(name);
        var updated = await _workspaceRepository.SaveAsync(workspace, ct);
        await AppendWorkspaceLogAsync(updated, ResourceLogType.System, "Workspace updated.", ct);
        await InvalidateUserAsync(userId, ct);
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
        var workspace = await RequireWorkspaceAdminAsync(userId, id, ct);
        if (workspace.IsDefault)
            throw new InvalidOperationException("Default workspaces cannot be deleted.");

        var deleted = await _workspaceRepository.DeleteAsync(id, ct);
        if (deleted)
        {
            await _workspaceRepository.GetCurrentAsync(userId, ct);
            await AppendWorkspaceLogAsync(workspace, ResourceLogType.System, "Workspace deleted.", ct);
            await InvalidateUserAsync(userId, ct);
        }

        return deleted;
    }

    public async Task<WorkspaceRecord> RequireAccessibleAsync(Guid userId, Guid workspaceId, CancellationToken ct = default)
    {
        return await _workspaceRepository.GetAccessibleAsync(userId, workspaceId, ct)
            ?? throw new InvalidOperationException("Workspace not found.");
    }

    public async Task<IReadOnlyList<WorkspaceMemberRecord>> ListMembersAsync(Guid actorUserId, Guid workspaceId, CancellationToken ct = default)
    {
        await RequireAccessibleAsync(actorUserId, workspaceId, ct);
        return await _workspaceMemberRepository.ListAsync(new WorkspaceMemberFilter { WorkspaceId = workspaceId }, ct);
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
        await AppendWorkspaceBindingLogAsync(workspaceId, memberUserId, $"Workspace member added as {member.Role}.", ct);
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

        existing.Role = ParseWorkspaceRole(role, WorkspaceRole.Editor);
        var updated = await _workspaceMemberRepository.UpsertAsync(existing, ct);
        await AppendWorkspaceBindingLogAsync(workspaceId, memberUserId, $"Workspace member role updated to {updated.Role}.", ct);
        return updated;
    }

    public async Task<bool> RemoveMemberAsync(Guid actorUserId, Guid workspaceId, Guid memberUserId, CancellationToken ct = default)
    {
        await RequireWorkspaceAdminAsync(actorUserId, workspaceId, ct);
        if (actorUserId == memberUserId)
            throw new InvalidOperationException("Workspace admins cannot remove themselves.");

        var removed = await _workspaceMemberRepository.DeleteAsync(
            new WorkspaceMemberFilter { WorkspaceId = workspaceId, UserId = memberUserId },
            ct);
        if (removed)
            await AppendWorkspaceBindingLogAsync(workspaceId, memberUserId, "Workspace member removed.", ct);

        return removed;
    }

    private async Task<WorkspaceRecord> RequireWorkspaceAdminAsync(Guid userId, Guid workspaceId, CancellationToken ct)
    {
        var workspace = await RequireAccessibleAsync(userId, workspaceId, ct);
        var membership = await _workspaceMemberRepository.GetByAsync(
            new WorkspaceMemberFilter { WorkspaceId = workspaceId, UserId = userId },
            ct);
        if (membership?.Role.CanAdminister() == true)
            return workspace;

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

    private Task AppendWorkspaceLogAsync(WorkspaceRecord workspace, ResourceLogType type, string content, CancellationToken ct)
        => _resourceLogService.AppendAsync(new ResourceLogRecord
        {
            WorkspaceId = workspace.Id,
            ResourceKind = ResourceLogKinds.Workspace,
            ResourceId = workspace.Id,
            ResourceName = workspace.Name,
            Type = type,
            Content = content,
        }, ct);

    private Task AppendWorkspaceBindingLogAsync(Guid workspaceId, Guid userId, string content, CancellationToken ct)
        => _resourceLogService.AppendAsync(new ResourceLogRecord
        {
            WorkspaceId = workspaceId,
            ResourceKind = ResourceLogKinds.WorkspaceBinding,
            ResourceId = userId,
            ResourceName = userId.ToString("N"),
            ParentResourceKind = ResourceLogKinds.Workspace,
            ParentResourceId = workspaceId,
            Type = ResourceLogType.System,
            Content = content,
            MetadataJson = JsonSerializer.Serialize(new { workspaceId, userId }),
        }, ct);

    private async Task InvalidateUserAsync(Guid userId, CancellationToken ct)
    {
        await _distributedCache.RemoveAsync($"auth:me:{userId}", ct);
    }
}
