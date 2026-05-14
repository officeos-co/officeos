namespace OffceOs.Application.Features.Management;

public sealed record OrganizationAuditExportResult(
    string Content,
    string ContentType,
    string FileName);
