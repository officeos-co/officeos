namespace EnterpriseAgentOs.Infrastructure.Features.AgentLogs;

internal sealed class AgentLogRepository : IAgentLogRepository
{
    private readonly EaosDbContext _eaosDbContext;

    public AgentLogRepository(EaosDbContext db) => _eaosDbContext = db;

    public async Task<List<AgentLogRecord>> ListAsync(Guid agentId, DateTime? before, int limit, CancellationToken ct = default)
    {
        var q = _eaosDbContext.AgentLogs.Where(l => l.AgentId == agentId);
        if (before.HasValue) q = q.Where(l => l.Time < before.Value);
        var entities = await q.OrderByDescending(l => l.Time).Take(limit).ToListAsync(ct);
        return entities.Select(ToAgentLogRecord).ToList();
    }

    public async Task<(List<GlobalLogRow> Items, int Total)> ListGlobalAsync(
        string? search, string? agentName, AgentLogType? type, int skip, int limit, CancellationToken ct = default)
    {
        var q = from l in _eaosDbContext.AgentLogs
                join a in _eaosDbContext.Agents on l.AgentId equals a.Id
                select new { Log = l, AgentName = a.Name };

        if (type.HasValue)
        {
            var t = type.Value;
            q = q.Where(x => x.Log.Type == t);
        }
        if (!string.IsNullOrWhiteSpace(agentName))
        {
            var needle = agentName.Trim();
            q = q.Where(x => EF.Functions.ILike(x.AgentName, $"%{needle}%"));
        }
        if (!string.IsNullOrWhiteSpace(search))
        {
            var needle = search.Trim();
            q = q.Where(x => EF.Functions.ILike(x.Log.Content, $"%{needle}%"));
        }

        var total = await q.CountAsync(ct);
        var rows = await q.OrderByDescending(x => x.Log.Time).Skip(skip).Take(limit).ToListAsync(ct);
        return (rows.Select(x => new GlobalLogRow(ToAgentLogRecord(x.Log), x.AgentName)).ToList(), total);
    }

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

    public async Task<AgentLogRecord?> GetByIdAsync(Guid id, CancellationToken ct = default)
    {
        var entity = await _eaosDbContext.AgentLogs.FirstOrDefaultAsync(l => l.Id == id, ct);
        return entity is null ? null : ToAgentLogRecord(entity);
    }

    public async Task<(List<AgentLogRecord> Items, int Total)> GetToolCallsAsync(
        Guid agentId, int limit, int offset, CancellationToken ct = default)
    {
        var query = _eaosDbContext.AgentLogs
            .Where(r => r.AgentId == agentId && r.Type == AgentLogType.ToolCall)
            .OrderByDescending(r => r.Time);

        var total = await query.CountAsync(ct);
        var entities = await query.Skip(offset).Take(limit).ToListAsync(ct);
        return (entities.Select(ToAgentLogRecord).ToList(), total);
    }

    public async Task<Dictionary<string, AgentLogRecord>> GetResultsByCorrelationAsync(
        Guid agentId, IReadOnlyCollection<string> correlationIds, CancellationToken ct = default)
    {
        if (correlationIds.Count == 0)
            return new Dictionary<string, AgentLogRecord>();

        var entities = await _eaosDbContext.AgentLogs
            .Where(r => r.AgentId == agentId
                        && r.Type == AgentLogType.ToolResult
                        && r.CorrelationId != null
                        && correlationIds.Contains(r.CorrelationId))
            .ToListAsync(ct);

        return entities
            .Select(ToAgentLogRecord)
            .Where(r => r.CorrelationId is not null)
            .GroupBy(r => r.CorrelationId!)
            .ToDictionary(g => g.Key, g => g.First());
    }

    public async Task DeleteByAgentIdsAsync(IReadOnlyList<Guid> agentIds, CancellationToken ct = default)
    {
        if (agentIds.Count == 0) return;
        await _eaosDbContext.AgentLogs.Where(l => agentIds.Contains(l.AgentId)).ExecuteDeleteAsync(ct);
    }

    public async Task<List<AgentLogRecord>> ListByAgentIdsAsync(IReadOnlyList<Guid> agentIds, IReadOnlyList<AgentLogType>? types = null, CancellationToken ct = default)
    {
        if (agentIds.Count == 0) return new List<AgentLogRecord>();
        var q = _eaosDbContext.AgentLogs.AsNoTracking().Where(l => agentIds.Contains(l.AgentId));
        if (types is { Count: > 0 }) q = q.Where(l => types.Contains(l.Type));
        var entities = await q.ToListAsync(ct);
        return entities.Select(ToAgentLogRecord).ToList();
    }

    private static AgentLogRecord ToAgentLogRecord(AgentLogEntity e) => new()
    {
        Id = e.Id,
        AgentId = e.AgentId,
        Time = e.Time,
        Type = e.Type,
        Tool = e.Tool,
        Integration = e.Integration,
        Channel = e.Channel,
        Content = e.Content,
        DurationMs = e.DurationMs,
        InputTokens = e.InputTokens,
        OutputTokens = e.OutputTokens,
        CorrelationId = e.CorrelationId,
        Agent = null,
    };

    private static AgentLogEntity ToAgentLogEntity(AgentLogRecord r) => new()
    {
        Id = r.Id,
        AgentId = r.AgentId,
        Time = r.Time,
        Type = r.Type,
        Tool = r.Tool,
        Integration = r.Integration,
        Channel = r.Channel,
        Content = r.Content,
        DurationMs = r.DurationMs,
        InputTokens = r.InputTokens,
        OutputTokens = r.OutputTokens,
        CorrelationId = r.CorrelationId,
    };
}
