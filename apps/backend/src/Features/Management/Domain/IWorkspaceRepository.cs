namespace OffceOs.Features.Management.Domain;

public interface IWorkspaceRepository
{
    Task<IReadOnlyList<WorkspaceRecord>> ListAsync(WorkspaceFilter filter, CancellationToken ct = default);
    Task<IReadOnlyList<WorkspaceRecord>> ListAccessibleAsync(Guid userId, CancellationToken ct = default);
    Task<WorkspaceRecord?> GetByAsync(WorkspaceFilter filter, CancellationToken ct = default);
    Task<WorkspaceRecord?> GetAccessibleAsync(Guid userId, Guid workspaceId, CancellationToken ct = default);
    Task<WorkspaceRecord> SaveAsync(WorkspaceRecord record, CancellationToken ct = default);
    Task<bool> DeleteAsync(Guid id, CancellationToken ct = default);
    Task<WorkspaceRecord> EnsurePersonalDefaultAsync(Guid userId, CancellationToken ct = default);
    Task<WorkspaceRecord> GetCurrentAsync(Guid userId, CancellationToken ct = default);
    Task SetCurrentAsync(Guid userId, Guid workspaceId, CancellationToken ct = default);
}
