namespace OffceOs.Infrastructure.Features.Management;

internal sealed class OrganizationAuditLogRepository : IOrganizationAuditLogRepository
{
    private readonly EaosDbContext _eaosDbContext;

    public OrganizationAuditLogRepository(EaosDbContext eaosDbContext)
    {
        _eaosDbContext = eaosDbContext;
    }

    public async Task<IReadOnlyList<OrganizationAuditLogRecord>> ListAsync(
        OrganizationAuditLogFilter filter,
        CancellationToken ct = default)
    {
        var query = _eaosDbContext.OrganizationAuditLogs
            .AsNoTracking()
            .Where(log => log.OrganizationId == filter.OrganizationId);

        if (filter.From.HasValue)
            query = query.Where(log => log.OccurredAt >= filter.From.Value);

        if (filter.To.HasValue)
            query = query.Where(log => log.OccurredAt <= filter.To.Value);

        if (!string.IsNullOrWhiteSpace(filter.Action))
            query = query.Where(log => log.Action == filter.Action.Trim());

        if (filter.ActorUserId.HasValue)
            query = query.Where(log => log.ActorUserId == filter.ActorUserId.Value);

        if (filter.WorkspaceId.HasValue)
            query = query.Where(log => log.WorkspaceId == filter.WorkspaceId.Value);

        if (filter.AgentId.HasValue)
            query = query.Where(log => log.AgentId == filter.AgentId.Value);

        if (!string.IsNullOrWhiteSpace(filter.Outcome))
            query = query.Where(log => log.Outcome == filter.Outcome.Trim());

        if (!string.IsNullOrWhiteSpace(filter.Search))
        {
            var search = filter.Search.Trim();
            query = query.Where(log =>
                log.Action.Contains(search)
                || log.ResourceType.Contains(search)
                || (log.ResourceId != null && log.ResourceId.Contains(search))
                || log.MetadataJson.Contains(search));
        }

        var limit = Math.Clamp(filter.Limit, 1, 1000);
        return await query
            .OrderByDescending(log => log.OccurredAt)
            .ThenByDescending(log => log.Id)
            .Take(limit)
            .Select(log => ToRecord(log))
            .ToListAsync(ct);
    }

    public async Task<OrganizationAuditLogRecord> SaveAsync(
        OrganizationAuditLogRecord record,
        CancellationToken ct = default)
    {
        var entity = new OrganizationAuditLogEntity
        {
            Id = record.Id == Guid.Empty ? Guid.NewGuid() : record.Id,
            OrganizationId = record.OrganizationId,
            ActorUserId = record.ActorUserId,
            WorkspaceId = record.WorkspaceId,
            AgentId = record.AgentId,
            Action = record.Action,
            ResourceType = record.ResourceType,
            ResourceId = record.ResourceId,
            Outcome = record.Outcome,
            CorrelationId = record.CorrelationId,
            MetadataJson = record.MetadataJson,
            OccurredAt = record.OccurredAt,
        };
        _eaosDbContext.OrganizationAuditLogs.Add(entity);
        await _eaosDbContext.SaveChangesAsync(ct);
        return ToRecord(entity);
    }

    private static OrganizationAuditLogRecord ToRecord(OrganizationAuditLogEntity entity) => new()
    {
        Id = entity.Id,
        OrganizationId = entity.OrganizationId,
        ActorUserId = entity.ActorUserId,
        WorkspaceId = entity.WorkspaceId,
        AgentId = entity.AgentId,
        Action = entity.Action,
        ResourceType = entity.ResourceType,
        ResourceId = entity.ResourceId,
        Outcome = entity.Outcome,
        CorrelationId = entity.CorrelationId,
        MetadataJson = entity.MetadataJson,
        OccurredAt = entity.OccurredAt,
    };
}
