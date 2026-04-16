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

    public Task<EnterpriseAgentOs.Api.Database.Models.AgentLogRecord?> GetByIdAsync(Guid id, CancellationToken ct = default)
        => _db.AgentLogs.FirstOrDefaultAsync(l => l.Id == id, ct);
}
