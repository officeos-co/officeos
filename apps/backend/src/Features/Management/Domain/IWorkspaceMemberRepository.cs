namespace OffceOs.Domain.Features.Management;

public interface IWorkspaceMemberRepository
{
    Task<IReadOnlyList<WorkspaceMemberRecord>> ListAsync(WorkspaceMemberFilter filter, CancellationToken ct = default);
    Task<WorkspaceMemberRecord?> GetByAsync(WorkspaceMemberFilter filter, CancellationToken ct = default);
    Task<WorkspaceMemberRecord> UpsertAsync(WorkspaceMemberRecord record, CancellationToken ct = default);
    Task<bool> DeleteAsync(WorkspaceMemberFilter filter, CancellationToken ct = default);
}
