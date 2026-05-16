namespace OffceOs.Features.Management.Domain;

public interface IWorkspaceService
{
    Task<IReadOnlyList<WorkspaceRecord>> ListAsync(Guid userId, CancellationToken ct = default);
    Task<WorkspaceRecord> GetCurrentAsync(Guid userId, CancellationToken ct = default);
    Task<WorkspaceRecord> CreateAsync(Guid userId, string? name, CancellationToken ct = default);
    Task<WorkspaceRecord> UpdateAsync(Guid userId, Guid id, string? name, CancellationToken ct = default);
    Task<WorkspaceRecord> SwitchAsync(Guid userId, Guid id, CancellationToken ct = default);
    Task<bool> DeleteAsync(Guid userId, Guid id, CancellationToken ct = default);
    Task<WorkspaceRecord> RequireAccessibleAsync(Guid userId, Guid workspaceId, CancellationToken ct = default);
    Task<IReadOnlyList<WorkspaceMemberRecord>> ListMembersAsync(Guid actorUserId, Guid workspaceId, CancellationToken ct = default);
    Task<WorkspaceMemberRecord> AddMemberAsync(Guid actorUserId, Guid workspaceId, Guid memberUserId, string? role, CancellationToken ct = default);
    Task<WorkspaceMemberRecord> UpdateMemberRoleAsync(Guid actorUserId, Guid workspaceId, Guid memberUserId, string? role, CancellationToken ct = default);
    Task<bool> RemoveMemberAsync(Guid actorUserId, Guid workspaceId, Guid memberUserId, CancellationToken ct = default);
}
