namespace EnterpriseAgentOs.Domain.Interfaces.Gdpr;

public interface IGdprService
{
    /// <summary>Exports all data owned by the given user as a structured DTO.</summary>
    Task<GdprExportDto> ExportAsync(Guid userId, CancellationToken ct = default);

    /// <summary>
    /// Permanently deletes all data owned by the given user in dependency order.
    /// </summary>
    Task PurgeAsync(Guid userId, CancellationToken ct = default);
}
