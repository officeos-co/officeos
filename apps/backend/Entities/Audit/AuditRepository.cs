namespace EnterpriseAgentOs.Api.Entities.Audit;

public sealed class AuditRepository : IAuditRepository
{
    private readonly EnterpriseAgentOs.Api.Database.EaosDbContext _db;

    public AuditRepository(EnterpriseAgentOs.Api.Database.EaosDbContext db)
    {
        _db = db;
    }

    public async Task AddPairAsync(EnterpriseAgentOs.Api.Database.Models.AgentLogRecord toolCall, EnterpriseAgentOs.Api.Database.Models.AgentLogRecord toolResult, CancellationToken ct = default)
    {
        _db.AgentLogs.Add(toolCall);
        _db.AgentLogs.Add(toolResult);
        await _db.SaveChangesAsync(ct);
    }

    public async Task<(List<EnterpriseAgentOs.Api.Database.Models.AgentLogRecord> Items, int Total)> GetByAgentAsync(
        Guid agentId, int limit, int offset, CancellationToken ct = default)
    {
        // Audit log = ToolCall entries only (one row per skill execution).
        var query = _db.AgentLogs
            .Where(r => r.AgentId == agentId && r.Type == EnterpriseAgentOs.Api.Database.Models.AgentLogType.ToolCall)
            .OrderByDescending(r => r.Time);

        var total = await query.CountAsync(ct);
        var items = await query.Skip(offset).Take(limit).ToListAsync(ct);

        return (items, total);
    }

    /// <summary>
    /// Returns the matching ToolResult record (by CorrelationId) for each ToolCall id supplied.
    /// </summary>
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
