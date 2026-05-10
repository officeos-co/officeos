namespace OffceOs.Domain.Features.Management;

public interface IOrganizationAuditLogRepository
{
    Task<IReadOnlyList<OrganizationAuditLogRecord>> ListAsync(OrganizationAuditLogFilter filter, CancellationToken ct = default);
    Task<OrganizationAuditLogRecord> SaveAsync(OrganizationAuditLogRecord record, CancellationToken ct = default);
}
