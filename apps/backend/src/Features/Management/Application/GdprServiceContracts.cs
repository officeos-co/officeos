namespace EnterpriseAgentOs.Application.Features.Management;

public interface IGdprService
{
    Task<GdprExportDto> ExportAsync(Guid userId, CancellationToken ct = default);
    Task PurgeAsync(Guid userId, CancellationToken ct = default);
}
