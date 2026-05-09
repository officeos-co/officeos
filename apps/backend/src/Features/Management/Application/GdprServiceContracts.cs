namespace OffceOs.Application.Features.Management;

public interface IGdprService
{
    Task<GdprExport> ExportAsync(Guid userId, CancellationToken ct = default);
    Task PurgeAsync(Guid userId, CancellationToken ct = default);
}
