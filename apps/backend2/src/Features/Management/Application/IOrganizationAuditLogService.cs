namespace OffceOs.Application.Features.Management;

public interface IOrganizationAuditLogService
{
    Task<IReadOnlyList<OrganizationAuditLogRecord>> ListAsync(
        Guid actorUserId,
        OrganizationAuditLogFilter filter,
        CancellationToken ct = default);

    Task<OrganizationAuditExportResult> ExportAsync(
        Guid actorUserId,
        OrganizationAuditLogFilter filter,
        string format,
        CancellationToken ct = default);
}
