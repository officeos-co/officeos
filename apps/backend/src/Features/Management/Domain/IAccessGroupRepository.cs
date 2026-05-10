namespace OffceOs.Domain.Features.Management;

public interface IAccessGroupRepository
{
    Task<IReadOnlyList<AccessGroupRecord>> ListAsync(AccessGroupFilter filter, CancellationToken ct = default);
    Task<AccessGroupRecord?> GetByAsync(AccessGroupFilter filter, CancellationToken ct = default);
    Task<AccessGroupRecord> SaveAsync(AccessGroupRecord record, CancellationToken ct = default);
    Task<bool> DeleteAsync(AccessGroupFilter filter, CancellationToken ct = default);
    Task<IReadOnlyList<AccessGroupMemberRecord>> ListMembersAsync(AccessGroupFilter filter, CancellationToken ct = default);
    Task<AccessGroupMemberRecord> AddMemberAsync(Guid accessGroupId, Guid userId, CancellationToken ct = default);
    Task<bool> RemoveMemberAsync(Guid accessGroupId, Guid userId, CancellationToken ct = default);
    Task<IReadOnlyList<AccessGroupWorkspaceGrantRecord>> ListWorkspaceGrantsAsync(AccessGroupFilter filter, CancellationToken ct = default);
    Task<AccessGroupWorkspaceGrantRecord> UpsertWorkspaceGrantAsync(AccessGroupWorkspaceGrantRecord record, CancellationToken ct = default);
    Task<bool> DeleteWorkspaceGrantAsync(Guid accessGroupId, Guid workspaceId, CancellationToken ct = default);
}
