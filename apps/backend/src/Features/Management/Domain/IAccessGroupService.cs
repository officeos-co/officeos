namespace OffceOs.Domain.Features.Management;

public interface IAccessGroupService
{
    Task<IReadOnlyList<AccessGroupRecord>> ListAsync(Guid actorUserId, Guid organizationId, CancellationToken ct = default);
    Task<AccessGroupRecord> CreateAsync(Guid actorUserId, Guid organizationId, string name, CancellationToken ct = default);
    Task<AccessGroupRecord> RenameAsync(Guid actorUserId, Guid accessGroupId, string name, CancellationToken ct = default);
    Task<bool> DeleteAsync(Guid actorUserId, Guid accessGroupId, CancellationToken ct = default);
    Task<AccessGroupMemberRecord> AddMemberAsync(Guid actorUserId, Guid accessGroupId, Guid userId, CancellationToken ct = default);
    Task<bool> RemoveMemberAsync(Guid actorUserId, Guid accessGroupId, Guid userId, CancellationToken ct = default);
    Task<AccessGroupWorkspaceGrantRecord> GrantWorkspaceAsync(Guid actorUserId, Guid accessGroupId, Guid workspaceId, string? role, CancellationToken ct = default);
    Task<bool> RevokeWorkspaceAsync(Guid actorUserId, Guid accessGroupId, Guid workspaceId, CancellationToken ct = default);
}
