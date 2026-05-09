namespace OffceOs.Domain.Features.Management;

public interface IWorkspaceRepository
{
    Task<IReadOnlyList<WorkspaceRecord>> ListAsync(WorkspaceFilter filter, CancellationToken ct = default);
    Task<WorkspaceRecord?> GetByAsync(WorkspaceFilter filter, CancellationToken ct = default);
    Task<WorkspaceRecord> SaveAsync(WorkspaceRecord record, CancellationToken ct = default);
    Task<bool> DeleteAsync(Guid id, Guid userId, CancellationToken ct = default);
    Task<WorkspaceRecord> EnsureDefaultAsync(Guid userId, CancellationToken ct = default);
    Task<WorkspaceRecord> GetCurrentAsync(Guid userId, CancellationToken ct = default);
    Task SetCurrentAsync(Guid userId, Guid workspaceId, CancellationToken ct = default);
}
