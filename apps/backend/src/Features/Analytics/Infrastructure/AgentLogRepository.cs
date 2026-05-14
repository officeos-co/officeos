namespace OffceOs.Infrastructure.Features.Analytics;

internal sealed class AgentLogRepository : IAgentLogRepository
{
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

        if (filter.RunId.HasValue)
            query = query.Where(l => l.RunId == filter.RunId.Value);

        if (filter.Type.HasValue)
            query = query.Where(l => l.Type == filter.Type.Value);

        if (filter.Types is not null)
            query = filter.Types.Count == 0
                ? query.Where(_ => false)
                : query.Where(l => filter.Types.Contains(l.Type));

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

    public async Task<List<UsageAggregateRow>> ListUsageAggregatesAsync(
        Guid ownerId,
        DateTime fromInclusive,
        DateTime toExclusive,
        CancellationToken ct = default)
    {
        var rows = await _eaosDbContext.ResourceLogs
            .AsNoTracking()
            .Where(l =>
                l.Agent != null &&
                l.Agent.OwnerId == ownerId &&
                l.Type == AgentLogType.System &&
                l.Time >= fromInclusive &&
                l.Time < toExclusive &&
                ((l.InputTokens ?? 0) + (l.OutputTokens ?? 0)) > 0)
            .Select(l => new UsageLogRow(
                l.Time,
                l.Tool,
                l.Agent!.Model,
                l.InputTokens ?? 0,
                l.OutputTokens ?? 0))
            .ToListAsync(ct);

        return rows
            .GroupBy(l => new
            {
                Date = DateTime.SpecifyKind(l.Time.Date, DateTimeKind.Utc),
                Model = ResolveUsageModel(l.Tool, l.AgentModel),
            })
            .Select(g => new UsageAggregateRow(
                g.Key.Date,
                g.Key.Model,
                g.Sum(l => (long)l.InputTokens),
                g.Sum(l => (long)l.OutputTokens)))
            .OrderBy(r => r.Date)
            .ThenBy(r => r.Model)
            .ToList();
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
            RunId = log.RunId,
            ParentRunId = log.ParentRunId,
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
            if (resourceId is null && !r.RunId.HasValue)
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

        if (r.RunId.HasValue)
        {
            resourceKind = ResourceLogKinds.Run;
            resourceId = r.RunId.Value;
            resourceName ??= r.RunId.Value.ToString("N");
            if (r.AgentId.HasValue)
            {
                parentResourceKind ??= ResourceLogKinds.Agent;
                parentResourceId ??= r.AgentId.Value;
            }
        }
        else if (r.ChannelConnectionId.HasValue)
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
            RunId = r.RunId,
            ParentRunId = r.ParentRunId,
        };
    }

    private static string ResolveUsageModel(string? loggedModel, string? agentModel)
    {
        if (!string.IsNullOrWhiteSpace(loggedModel))
            return loggedModel;

        if (!string.IsNullOrWhiteSpace(agentModel))
            return agentModel;

        return ProviderRegistry.DefaultModel;
    }

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

    private sealed record UsageLogRow(
        DateTime Time,
        string? Tool,
        string? AgentModel,
        int InputTokens,
        int OutputTokens);
}
