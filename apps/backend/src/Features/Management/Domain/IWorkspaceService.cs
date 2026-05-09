namespace OffceOs.Domain.Features.Management;

public interface IWorkspaceService
{
    Task<IReadOnlyList<WorkspaceRecord>> ListAsync(Guid userId, CancellationToken ct = default);
    Task<WorkspaceRecord> GetCurrentAsync(Guid userId, CancellationToken ct = default);
    Task<WorkspaceRecord> CreateAsync(Guid userId, string? name, CancellationToken ct = default);
    Task<WorkspaceRecord> UpdateAsync(Guid userId, Guid id, string? name, CancellationToken ct = default);
    Task<WorkspaceRecord> SwitchAsync(Guid userId, Guid id, CancellationToken ct = default);
    Task<bool> DeleteAsync(Guid userId, Guid id, CancellationToken ct = default);
}
