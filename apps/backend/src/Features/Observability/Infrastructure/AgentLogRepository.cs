namespace OffceOs.Infrastructure.Features.Observability;

internal sealed class AgentLogRepository : IAgentLogRepository
{
    private static readonly SemaphoreSlim WorkQueueLock = new(1, 1);
    private readonly EaosDbContext _eaosDbContext;

    public AgentLogRepository(EaosDbContext db) => _eaosDbContext = db;

    public IQueryable<AgentLogRecord> Query(AgentLogFilter filter)
    {
        var query = _eaosDbContext.ResourceLogs.AsNoTracking().AsQueryable();

        if (filter.Id.HasValue)
            query = query.Where(l => l.Id == filter.Id.Value);

        if (!string.IsNullOrWhiteSpace(filter.ResourceKind))
            query = query.Where(l => l.ResourceKind == filter.ResourceKind.Trim());

        if (filter.ResourceId.HasValue)
            query = query.Where(l => l.ResourceId == filter.ResourceId.Value);

        if (!string.IsNullOrWhiteSpace(filter.ResourceName))
        {
            var resourceName = filter.ResourceName.Trim().ToLower();
            query = query.Where(l => l.ResourceName != null && l.ResourceName.ToLower() == resourceName);
        }

        if (!string.IsNullOrWhiteSpace(filter.Severity))
            query = query.Where(l => l.Severity == filter.Severity.Trim().ToLowerInvariant());

        if (filter.AgentId.HasValue)
            query = query.Where(l => l.AgentId == filter.AgentId.Value);

        if (filter.AgentIds is not null)
            query = filter.AgentIds.Count == 0
                ? query.Where(_ => false)
                : query.Where(l => l.AgentId.HasValue && filter.AgentIds.Contains(l.AgentId.Value));

        if (filter.OwnerId.HasValue)
            query = query.Where(l => l.Agent != null && l.Agent.OwnerId == filter.OwnerId.Value);

        if (filter.WorkspaceId.HasValue)
            query = query.Where(l => l.WorkspaceId == filter.WorkspaceId.Value
                || (l.Agent != null && l.Agent.WorkspaceId == filter.WorkspaceId.Value));

        if (filter.ChannelConnectionId.HasValue)
            query = query.Where(l => l.ChannelConnectionId == filter.ChannelConnectionId.Value);

        if (!string.IsNullOrEmpty(filter.CorrelationId))
            query = query.Where(l => l.CorrelationId == filter.CorrelationId);

        if (filter.CorrelationIds is not null)
            query = filter.CorrelationIds.Count == 0
                ? query.Where(_ => false)
                : query.Where(l => l.CorrelationId != null && filter.CorrelationIds.Contains(l.CorrelationId));

        if (filter.Type.HasValue)
            query = query.Where(l => l.Type == filter.Type.Value);

        if (filter.Types is not null)
            query = filter.Types.Count == 0
                ? query.Where(_ => false)
                : query.Where(l => filter.Types.Contains(l.Type));

        if (!string.IsNullOrWhiteSpace(filter.WorkStatus))
            query = query.Where(l => l.WorkStatus == filter.WorkStatus.Trim().ToLowerInvariant());

        if (!string.IsNullOrWhiteSpace(filter.WorkPurpose))
            query = query.Where(l => l.WorkPurpose == filter.WorkPurpose.Trim().ToLowerInvariant());

        if (filter.DefinitionId.HasValue)
            query = query.Where(l => l.DefinitionId == filter.DefinitionId.Value);

        if (filter.HasWorkStatus.HasValue)
            query = filter.HasWorkStatus.Value
                ? query.Where(l => l.WorkStatus != null)
                : query.Where(l => l.WorkStatus == null);

        if (!string.IsNullOrWhiteSpace(filter.AgentName))
        {
            var needle = filter.AgentName.Trim();
            query = query.Where(l => l.Agent != null && EF.Functions.ILike(l.Agent.Name, $"%{needle}%"));
        }

        if (!string.IsNullOrWhiteSpace(filter.Search))
        {
            var needle = filter.Search.Trim();
            query = query.Where(l => EF.Functions.ILike(l.Content, $"%{needle}%"));
        }

        if (!string.IsNullOrWhiteSpace(filter.ContentStartsWith))
            query = query.Where(l => l.Content.StartsWith(filter.ContentStartsWith));

        if (filter.FromInclusive.HasValue)
            query = query.Where(l => l.Time >= filter.FromInclusive.Value);

        if (filter.ToExclusive.HasValue)
            query = query.Where(l => l.Time < filter.ToExclusive.Value);

        if (filter.Before.HasValue)
            query = query.Where(l => l.Time < filter.Before.Value);

        return ProjectAgentLogRecords(query);
    }

    public async Task<List<AgentLogRecord>> ListAsync(
        AgentLogFilter filter,
        AgentLogListOptions? options = null,
        CancellationToken ct = default)
    {
        options ??= new AgentLogListOptions();
        var query = Query(filter);

        if (options.AfterLogId.HasValue)
        {
            var boundary = await _eaosDbContext.ResourceLogs.AsNoTracking()
                .Where(l => l.Id == options.AfterLogId.Value)
                .Select(l => (DateTime?)l.Time)
                .FirstOrDefaultAsync(ct);
            if (boundary.HasValue)
                query = query.Where(l => l.Time > boundary.Value);
        }

        query = options.Sort switch
        {
            AgentLogSort.TimeAscending => query.OrderBy(l => l.Time).ThenBy(l => l.Id),
            _ => query.OrderByDescending(l => l.Time).ThenByDescending(l => l.Id),
        };

        if (options.Skip is > 0)
            query = query.Skip(options.Skip.Value);

        if (options.Limit is > 0)
            query = query.Take(options.Limit.Value);

        return await query.ToListAsync(ct);
    }

    public Task<int> CountAsync(AgentLogFilter filter, CancellationToken ct = default)
        => Query(filter).CountAsync(ct);

    public async Task<AgentLogRecord> AppendAsync(AgentLogRecord record, CancellationToken ct = default)
    {
        _eaosDbContext.ResourceLogs.Add(await ToAgentLogEntityAsync(record, ct));
        await _eaosDbContext.SaveChangesAsync(ct);
        return record;
    }

    public async Task AppendPairAsync(AgentLogRecord toolCall, AgentLogRecord toolResult, CancellationToken ct = default)
    {
        _eaosDbContext.ResourceLogs.Add(await ToAgentLogEntityAsync(toolCall, ct));
        _eaosDbContext.ResourceLogs.Add(await ToAgentLogEntityAsync(toolResult, ct));
        await _eaosDbContext.SaveChangesAsync(ct);
    }

    public Task<AgentLogRecord?> GetByAsync(AgentLogFilter filter, CancellationToken ct = default)
        => Query(filter).FirstOrDefaultAsync(ct);

    public async Task DeleteByAgentIdsAsync(IReadOnlyList<Guid> agentIds, CancellationToken ct = default)
    {
        if (agentIds.Count == 0) return;
        await _eaosDbContext.ResourceLogs
            .Where(l => l.AgentId.HasValue && agentIds.Contains(l.AgentId.Value))
            .ExecuteDeleteAsync(ct);
    }

    public async Task<AgentLogRecord> UpsertQueuedWorkAsync(AgentLogRecord record, CancellationToken ct = default)
    {
        var existing = string.IsNullOrWhiteSpace(record.CorrelationId)
            ? null
            : await _eaosDbContext.ResourceLogs
                .FirstOrDefaultAsync(l =>
                    l.AgentId == record.AgentId &&
                    l.CorrelationId == record.CorrelationId &&
                    l.Type == AgentLogType.MessageIn,
                    ct);

        if (existing is null)
        {
            _eaosDbContext.ResourceLogs.Add(await ToAgentLogEntityAsync(record, ct));
            await _eaosDbContext.SaveChangesAsync(ct);
            return record;
        }

        existing.WorkStatus ??= AgentWorkStatusKinds.Queued;
        existing.WorkPurpose ??= AgentWorkPurposeKinds.Normalize(record.WorkPurpose);
        existing.DefinitionId ??= record.DefinitionId;
        existing.WorkspaceId ??= record.WorkspaceId;
        existing.ResourceKind = ResourceLogKinds.Agent;
        existing.ResourceId = record.AgentId;
        existing.AgentId = record.AgentId;
        await _eaosDbContext.SaveChangesAsync(ct);
        return ToRecord(existing);
    }

    public async Task<AgentLogRecord?> ClaimNextQueuedWorkAsync(CancellationToken ct = default)
    {
        await WorkQueueLock.WaitAsync(ct);
        try
        {
            var runningAgentIds = await _eaosDbContext.ResourceLogs
                .Where(log => log.Type == AgentLogType.MessageIn
                    && log.WorkStatus == AgentWorkStatusKinds.Running
                    && log.AgentId.HasValue)
                .Select(log => log.AgentId!.Value)
                .Distinct()
                .ToListAsync(ct);

            var work = await _eaosDbContext.ResourceLogs
                .Where(log => log.Type == AgentLogType.MessageIn
                    && log.WorkStatus == AgentWorkStatusKinds.Queued
                    && log.AgentId.HasValue
                    && !runningAgentIds.Contains(log.AgentId.Value))
                .OrderBy(log => log.Time)
                .ThenBy(log => log.Id)
                .FirstOrDefaultAsync(ct);

            if (work is null)
                return null;

            var now = DateTime.UtcNow;
            work.WorkStatus = AgentWorkStatusKinds.Running;
            work.StartedAt ??= now;
            work.WorkError = null;
            await _eaosDbContext.SaveChangesAsync(ct);
            return ToRecord(work);
        }
        finally
        {
            WorkQueueLock.Release();
        }
    }

    public async Task MarkWorkAsync(Guid workLogId, string status, string? error = null, CancellationToken ct = default)
    {
        var work = await _eaosDbContext.ResourceLogs.FirstOrDefaultAsync(log => log.Id == workLogId, ct);
        if (work is null)
            return;

        var normalized = AgentWorkStatusKinds.Normalize(status);
        work.WorkStatus = normalized;
        work.WorkError = string.IsNullOrWhiteSpace(error) ? null : error;
        if (normalized == AgentWorkStatusKinds.Running)
            work.StartedAt ??= DateTime.UtcNow;
        if (normalized is AgentWorkStatusKinds.Completed or AgentWorkStatusKinds.Failed or AgentWorkStatusKinds.Canceled)
            work.CompletedAt = DateTime.UtcNow;

        await _eaosDbContext.SaveChangesAsync(ct);
    }

    private static IQueryable<AgentLogRecord> ProjectAgentLogRecords(IQueryable<ResourceLogEntity> query) =>
        query.Select(log => new AgentLogRecord
        {
            Id = log.Id,
            ResourceKind = log.ResourceKind,
            ResourceId = log.ResourceId,
            ResourceName = log.ResourceName,
            ParentResourceKind = log.ParentResourceKind,
            ParentResourceId = log.ParentResourceId,
            AgentId = log.AgentId,
            Agent = log.Agent == null
                ? null
                : new AgentRecord
                {
            Id = log.Agent.Id,
            Name = log.Agent.Name,
            Provider = log.Agent.Provider,
            Model = log.Agent.Model,
            OwnerId = log.Agent.OwnerId,
            WorkspaceId = log.Agent.WorkspaceId,
        },
        WorkspaceId = log.WorkspaceId,
            Time = log.Time,
            Type = log.Type,
            Severity = log.Severity,
            Tool = log.Tool,
            Integration = log.Integration,
            Channel = log.Channel,
            ChannelConnectionId = log.ChannelConnectionId,
            Content = log.Content,
            MetadataJson = log.MetadataJson,
            Usage = new TokenUsage(log.InputTokens, log.OutputTokens, log.DurationMs),
            CorrelationId = log.CorrelationId,
            WorkStatus = log.WorkStatus,
            WorkPurpose = log.WorkPurpose,
            DefinitionId = log.DefinitionId,
            StartedAt = log.StartedAt,
            CompletedAt = log.CompletedAt,
            WorkError = log.WorkError,
        });

    private async Task<ResourceLogEntity> ToAgentLogEntityAsync(AgentLogRecord r, CancellationToken ct)
    {
        var workspaceId = r.WorkspaceId;
        var resourceKind = r.ResourceKind;
        var resourceId = r.ResourceId;
        var resourceName = r.ResourceName;
        var parentResourceKind = r.ParentResourceKind;
        var parentResourceId = r.ParentResourceId;

        if (workspaceId is null && r.AgentId.HasValue)
        {
            var agent = await _eaosDbContext.Agents.AsNoTracking()
                .Where(a => a.Id == r.AgentId.Value)
                .Select(a => new { a.WorkspaceId, a.Name })
                .FirstOrDefaultAsync(ct);
            workspaceId = agent?.WorkspaceId;
            if (resourceId is null)
                resourceId = r.AgentId.Value;
            resourceName ??= agent?.Name;
        }

        if (workspaceId is null && r.ChannelConnectionId.HasValue)
        {
            var channel = await _eaosDbContext.ChannelConnections.AsNoTracking()
                .Where(c => c.Id == r.ChannelConnectionId.Value)
                .Select(c => new { c.WorkspaceId, c.DisplayName })
                .FirstOrDefaultAsync(ct);
            workspaceId = channel?.WorkspaceId;
            resourceName ??= channel?.DisplayName;
        }

        if (r.ChannelConnectionId.HasValue)
        {
            resourceKind = ResourceLogKinds.Channel;
            resourceId = r.ChannelConnectionId.Value;
        }
        else if (r.AgentId.HasValue)
        {
            resourceKind = ResourceLogKinds.Agent;
            resourceId ??= r.AgentId.Value;
        }

        return new ResourceLogEntity
        {
            Id = r.Id,
            ResourceKind = resourceKind,
            ResourceId = resourceId,
            ResourceName = resourceName,
            ParentResourceKind = parentResourceKind,
            ParentResourceId = parentResourceId,
            AgentId = r.AgentId,
            WorkspaceId = workspaceId,
            Time = r.Time,
            Type = r.Type,
            Severity = NormalizeSeverity(r),
            Tool = r.Tool,
            Integration = r.Integration,
            Channel = r.Channel,
            ChannelConnectionId = r.ChannelConnectionId,
            Content = r.Content,
            MetadataJson = r.MetadataJson,
            DurationMs = r.Usage.DurationMs,
            InputTokens = r.Usage.InputTokens,
            OutputTokens = r.Usage.OutputTokens,
            CorrelationId = r.CorrelationId,
            WorkStatus = string.IsNullOrWhiteSpace(r.WorkStatus) ? null : AgentWorkStatusKinds.Normalize(r.WorkStatus),
            WorkPurpose = string.IsNullOrWhiteSpace(r.WorkPurpose) ? null : AgentWorkPurposeKinds.Normalize(r.WorkPurpose),
            DefinitionId = r.DefinitionId,
            StartedAt = r.StartedAt,
            CompletedAt = r.CompletedAt,
            WorkError = r.WorkError,
        };
    }

    private static AgentLogRecord ToRecord(ResourceLogEntity log) => new()
    {
        Id = log.Id,
        ResourceKind = log.ResourceKind,
        ResourceId = log.ResourceId,
        ResourceName = log.ResourceName,
        ParentResourceKind = log.ParentResourceKind,
        ParentResourceId = log.ParentResourceId,
        AgentId = log.AgentId,
        WorkspaceId = log.WorkspaceId,
        Time = log.Time,
        Type = log.Type,
        Severity = log.Severity,
        Tool = log.Tool,
        Integration = log.Integration,
        Channel = log.Channel,
        ChannelConnectionId = log.ChannelConnectionId,
        Content = log.Content,
        MetadataJson = log.MetadataJson,
        Usage = new TokenUsage(log.InputTokens, log.OutputTokens, log.DurationMs),
        CorrelationId = log.CorrelationId,
        WorkStatus = log.WorkStatus,
        WorkPurpose = log.WorkPurpose,
        DefinitionId = log.DefinitionId,
        StartedAt = log.StartedAt,
        CompletedAt = log.CompletedAt,
        WorkError = log.WorkError,
    };

    private static string NormalizeSeverity(AgentLogRecord record)
    {
        if (!string.IsNullOrWhiteSpace(record.Severity) &&
            !record.Severity.Equals(ResourceLogSeverityKinds.Info, StringComparison.OrdinalIgnoreCase))
        {
            return record.Severity.Trim().ToLowerInvariant();
        }

        return record.Type.ToString().StartsWith("Error", StringComparison.Ordinal) || record.Type == AgentLogType.Error
            ? ResourceLogSeverityKinds.Error
            : ResourceLogSeverityKinds.Info;
    }

}
