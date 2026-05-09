namespace OffceOs.Infrastructure.Features.Analytics;

internal sealed class AgentLogRepository : IAgentLogRepository
{
    private readonly EaosDbContext _eaosDbContext;

    public AgentLogRepository(EaosDbContext db) => _eaosDbContext = db;

    public IQueryable<AgentLogRecord> Query(AgentLogFilter filter)
    {
        var query = _eaosDbContext.AgentLogs.AsNoTracking().AsQueryable();

        if (filter.Id.HasValue)
            query = query.Where(l => l.Id == filter.Id.Value);

        if (filter.AgentId.HasValue)
            query = query.Where(l => l.AgentId == filter.AgentId.Value);

        if (filter.AgentIds is not null)
            query = filter.AgentIds.Count == 0
                ? query.Where(_ => false)
                : query.Where(l => l.AgentId.HasValue && filter.AgentIds.Contains(l.AgentId.Value));

        if (filter.OwnerId.HasValue)
            query = query.Where(l => l.Agent != null && l.Agent.OwnerId == filter.OwnerId.Value);

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
            var boundary = await _eaosDbContext.AgentLogs.AsNoTracking()
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
        var rows = await _eaosDbContext.AgentLogs
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
        _eaosDbContext.AgentLogs.Add(ToAgentLogEntity(record));
        await _eaosDbContext.SaveChangesAsync(ct);
        return record;
    }

    public async Task AppendPairAsync(AgentLogRecord toolCall, AgentLogRecord toolResult, CancellationToken ct = default)
    {
        _eaosDbContext.AgentLogs.Add(ToAgentLogEntity(toolCall));
        _eaosDbContext.AgentLogs.Add(ToAgentLogEntity(toolResult));
        await _eaosDbContext.SaveChangesAsync(ct);
    }

    public Task<AgentLogRecord?> GetByAsync(AgentLogFilter filter, CancellationToken ct = default)
        => Query(filter).FirstOrDefaultAsync(ct);

    public async Task DeleteByAgentIdsAsync(IReadOnlyList<Guid> agentIds, CancellationToken ct = default)
    {
        if (agentIds.Count == 0) return;
        await _eaosDbContext.AgentLogs
            .Where(l => l.AgentId.HasValue && agentIds.Contains(l.AgentId.Value))
            .ExecuteDeleteAsync(ct);
    }

    private static IQueryable<AgentLogRecord> ProjectAgentLogRecords(IQueryable<AgentLogEntity> query) =>
        query.Select(log => new AgentLogRecord
        {
            Id = log.Id,
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
                },
            Time = log.Time,
            Type = log.Type,
            Tool = log.Tool,
            Integration = log.Integration,
            Channel = log.Channel,
            ChannelConnectionId = log.ChannelConnectionId,
            Content = log.Content,
            Usage = new TokenUsage(log.InputTokens, log.OutputTokens, log.DurationMs),
            CorrelationId = log.CorrelationId,
            RunId = log.RunId,
            ParentRunId = log.ParentRunId,
        });

    private static AgentLogEntity ToAgentLogEntity(AgentLogRecord r) => new()
    {
        Id = r.Id,
        AgentId = r.AgentId,
        Time = r.Time,
        Type = r.Type,
        Tool = r.Tool,
        Integration = r.Integration,
        Channel = r.Channel,
        ChannelConnectionId = r.ChannelConnectionId,
        Content = r.Content,
        DurationMs = r.Usage.DurationMs,
        InputTokens = r.Usage.InputTokens,
        OutputTokens = r.Usage.OutputTokens,
        CorrelationId = r.CorrelationId,
        RunId = r.RunId,
        ParentRunId = r.ParentRunId,
    };

    private static string ResolveUsageModel(string? loggedModel, string? agentModel)
    {
        if (!string.IsNullOrWhiteSpace(loggedModel))
            return loggedModel;

        if (!string.IsNullOrWhiteSpace(agentModel))
            return agentModel;

        return ProviderRegistry.DefaultModel;
    }

    private sealed record UsageLogRow(
        DateTime Time,
        string? Tool,
        string? AgentModel,
        int InputTokens,
        int OutputTokens);
}
