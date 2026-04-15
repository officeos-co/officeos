using EnterpriseAgentOs.Api.Database;
using EnterpriseAgentOs.Api.Database.Models;
using Microsoft.EntityFrameworkCore;

namespace EnterpriseAgentOs.Api.Entities.Audit;

public sealed class AuditRepository : IAuditRepository
{
    private readonly EaosDbContext _db;

    public AuditRepository(EaosDbContext db)
    {
        _db = db;
    }

    public async Task AddAsync(AgentToolCallRecord record, CancellationToken ct = default)
    {
        _db.AgentToolCalls.Add(record);
        await _db.SaveChangesAsync(ct);
    }

    public async Task<(List<AgentToolCallRecord> Items, int Total)> GetByAgentAsync(
        Guid agentId, int limit, int offset, CancellationToken ct = default)
    {
        var query = _db.AgentToolCalls
            .Where(r => r.AgentId == agentId)
            .OrderByDescending(r => r.Timestamp);

        var total = await query.CountAsync(ct);
        var items = await query.Skip(offset).Take(limit).ToListAsync(ct);

        return (items, total);
    }
}
