namespace EnterpriseAgentOs.Api.Entities.AgentLogs;

public sealed class AgentLogRepository : IAgentLogRepository
{
    private readonly EnterpriseAgentOs.Api.Database.EaosDbContext _db;

    public AgentLogRepository(EnterpriseAgentOs.Api.Database.EaosDbContext db) => _db = db;

    public async Task<List<EnterpriseAgentOs.Api.Database.Models.AgentLogRecord>> ListAsync(Guid agentId, DateTime? before, int limit, CancellationToken ct = default)
    {
        var q = _db.AgentLogs.Where(l => l.AgentId == agentId);
        if (before.HasValue) q = q.Where(l => l.Time < before.Value);
        return await q.OrderByDescending(l => l.Time).Take(limit).ToListAsync(ct);
    }

    public async Task<(List<GlobalLogRow> Items, int Total)> ListGlobalAsync(
        string? search, string? agentName, EnterpriseAgentOs.Api.Database.Models.AgentLogType? type, int skip, int limit, CancellationToken ct = default)
    {
        var q = from l in _db.AgentLogs
                join a in _db.Agents on l.AgentId equals a.Id
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
        return (rows.Select(x => new GlobalLogRow(x.Log, x.AgentName)).ToList(), total);
    }

    public async Task<EnterpriseAgentOs.Api.Database.Models.AgentLogRecord> AppendAsync(EnterpriseAgentOs.Api.Database.Models.AgentLogRecord record, CancellationToken ct = default)
    {
        _db.AgentLogs.Add(record);
        await _db.SaveChangesAsync(ct);
        return record;
    }

    public async Task AppendPairAsync(EnterpriseAgentOs.Api.Database.Models.AgentLogRecord toolCall, EnterpriseAgentOs.Api.Database.Models.AgentLogRecord toolResult, CancellationToken ct = default)
    {
        _db.AgentLogs.Add(toolCall);
        _db.AgentLogs.Add(toolResult);
        await _db.SaveChangesAsync(ct);
    }

    public Task<EnterpriseAgentOs.Api.Database.Models.AgentLogRecord?> GetByIdAsync(Guid id, CancellationToken ct = default)
        => _db.AgentLogs.FirstOrDefaultAsync(l => l.Id == id, ct);

    public async Task<(List<EnterpriseAgentOs.Api.Database.Models.AgentLogRecord> Items, int Total)> GetToolCallsAsync(
        Guid agentId, int limit, int offset, CancellationToken ct = default)
    {
        var query = _db.AgentLogs
            .Where(r => r.AgentId == agentId && r.Type == EnterpriseAgentOs.Api.Database.Models.AgentLogType.ToolCall)
            .OrderByDescending(r => r.Time);

        var total = await query.CountAsync(ct);
        var items = await query.Skip(offset).Take(limit).ToListAsync(ct);
        return (items, total);
    }

    public async Task<Dictionary<string, EnterpriseAgentOs.Api.Database.Models.AgentLogRecord>> GetResultsByCorrelationAsync(
        Guid agentId, IReadOnlyCollection<string> correlationIds, CancellationToken ct = default)
    {
        if (correlationIds.Count == 0)
            return new Dictionary<string, EnterpriseAgentOs.Api.Database.Models.AgentLogRecord>();

        var rows = await _db.AgentLogs
            .Where(r => r.AgentId == agentId
                        && r.Type == EnterpriseAgentOs.Api.Database.Models.AgentLogType.ToolResult
                        && r.CorrelationId != null
                        && correlationIds.Contains(r.CorrelationId))
            .ToListAsync(ct);

        return rows
            .Where(r => r.CorrelationId is not null)
            .GroupBy(r => r.CorrelationId!)
            .ToDictionary(g => g.Key, g => g.First());
    }
}
