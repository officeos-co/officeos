namespace OffceOs.Application.Features.Management;

internal sealed class OrganizationAuditLogService : IOrganizationAuditLogService
{
    private readonly IOrganizationAuditLogRepository _organizationAuditLogRepository;
    private readonly IOrganizationRepository _organizationRepository;

    public OrganizationAuditLogService(
        IOrganizationAuditLogRepository organizationAuditLogRepository,
        IOrganizationRepository organizationRepository)
    {
        _organizationAuditLogRepository = organizationAuditLogRepository;
        _organizationRepository = organizationRepository;
    }

    public async Task<IReadOnlyList<OrganizationAuditLogRecord>> ListAsync(
        Guid actorUserId,
        OrganizationAuditLogFilter filter,
        CancellationToken ct = default)
    {
        await RequireOrganizationAdminAsync(actorUserId, filter.OrganizationId, ct);
        var rows = await _organizationAuditLogRepository.ListAsync(filter, ct);
        return rows.Select(RedactRecord).ToList();
    }

    public async Task<OrganizationAuditExportResult> ExportAsync(
        Guid actorUserId,
        OrganizationAuditLogFilter filter,
        string format,
        CancellationToken ct = default)
    {
        var rows = await ListAsync(actorUserId, filter with { Limit = Math.Clamp(filter.Limit, 1, 1000) }, ct);
        var ordered = rows.OrderBy(row => row.OccurredAt).ThenBy(row => row.Id).ToList();
        return format.Trim().ToLowerInvariant() switch
        {
            "csv" => new OrganizationAuditExportResult(
                ToCsv(ordered),
                "text/csv",
                $"organization-audit-{filter.OrganizationId:N}.csv"),
            "jsonl" => new OrganizationAuditExportResult(
                ToJsonl(ordered),
                "application/x-ndjson",
                $"organization-audit-{filter.OrganizationId:N}.jsonl"),
            _ => throw new InvalidOperationException("Audit export format must be 'csv' or 'jsonl'."),
        };
    }

    private async Task RequireOrganizationAdminAsync(Guid userId, Guid organizationId, CancellationToken ct)
    {
        var members = await _organizationRepository.ListMembersAsync(organizationId, ct);
        var member = members.FirstOrDefault(item => item.UserId == userId && item.Status == MemberStatus.Active);
        if (member?.Role is not (OrgRole.Owner or OrgRole.Admin))
            throw new InvalidOperationException("Organization not found.");
    }

    private static OrganizationAuditLogRecord RedactRecord(OrganizationAuditLogRecord record)
        => record with { MetadataJson = OrganizationAuditMetadataPolicy.RedactJson(record.MetadataJson) };

    private static string ToJsonl(IReadOnlyList<OrganizationAuditLogRecord> rows)
    {
        var builder = new StringBuilder();
        foreach (var row in rows)
        {
            builder.Append(JsonSerializer.Serialize(ToExportObject(row)));
            builder.Append('\n');
        }

        return builder.ToString();
    }

    private static string ToCsv(IReadOnlyList<OrganizationAuditLogRecord> rows)
    {
        var builder = new StringBuilder();
        builder.AppendLine("id,occurredAt,organizationId,actorUserId,workspaceId,agentId,action,resourceType,resourceId,outcome,correlationId,metadata");
        foreach (var row in rows)
        {
            builder.AppendJoin(',', new[]
            {
                Csv(row.Id.ToString("N")),
                Csv(row.OccurredAt.ToUniversalTime().ToString("O")),
                Csv(row.OrganizationId.ToString("N")),
                Csv(row.ActorUserId?.ToString("N")),
                Csv(row.WorkspaceId?.ToString("N")),
                Csv(row.AgentId?.ToString("N")),
                Csv(row.Action),
                Csv(row.ResourceType),
                Csv(row.ResourceId),
                Csv(row.Outcome),
                Csv(row.CorrelationId),
                Csv(OrganizationAuditMetadataPolicy.RedactJson(row.MetadataJson)),
            });
            builder.Append('\n');
        }

        return builder.ToString();
    }

    private static object ToExportObject(OrganizationAuditLogRecord row) => new
    {
        id = row.Id,
        occurredAt = row.OccurredAt.ToUniversalTime(),
        organizationId = row.OrganizationId,
        actorUserId = row.ActorUserId,
        workspaceId = row.WorkspaceId,
        agentId = row.AgentId,
        action = row.Action,
        resourceType = row.ResourceType,
        resourceId = row.ResourceId,
        outcome = row.Outcome,
        correlationId = row.CorrelationId,
        metadata = JsonSerializer.Deserialize<JsonElement>(OrganizationAuditMetadataPolicy.RedactJson(row.MetadataJson)),
    };

    private static string Csv(string? value)
    {
        value ??= string.Empty;
        return "\"" + value.Replace("\"", "\"\"", StringComparison.Ordinal) + "\"";
    }
}
