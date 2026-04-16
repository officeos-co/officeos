namespace EnterpriseAgentOs.Api.Entities.Audit;

public sealed class AuditRepository : IAuditRepository
{
    private readonly EaosDbContext _db;

    public AuditRepository(EaosDbContext db)
    {
        _db = db;
    }

    public async Task AddPairAsync(AgentLogRecord toolCall, AgentLogRecord toolResult, CancellationToken ct = default)
    {
        _db.AgentLogs.Add(toolCall);
        _db.AgentLogs.Add(toolResult);
        await _db.SaveChangesAsync(ct);
    }

    public async Task<(List<AgentLogRecord> Items, int Total)> GetByAgentAsync(
        Guid agentId, int limit, int offset, CancellationToken ct = default)
    {
        // Audit log = ToolCall entries only (one row per skill execution).
        var query = _db.AgentLogs
            .Where(r => r.AgentId == agentId && r.Type == AgentLogType.ToolCall)
            .OrderByDescending(r => r.Time);

        var total = await query.CountAsync(ct);
        var items = await query.Skip(offset).Take(limit).ToListAsync(ct);

        return (items, total);
    }
}
