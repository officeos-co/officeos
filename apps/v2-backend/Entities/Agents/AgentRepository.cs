using EnterpriseAgentOs.Api.Database;
using EnterpriseAgentOs.Api.Entities.Agents;
using Microsoft.EntityFrameworkCore;

namespace EnterpriseAgentOs.Api.Entities.Agents;

public sealed class AgentRepository : IAgentRepository
{
    private readonly EaosDbContext _db;

    public AgentRepository(EaosDbContext db)
    {
        _db = db;
    }

    public async Task<IReadOnlyList<AgentRecord>> ListAsync(CancellationToken ct = default)
    {
        return await _db.Agents
            .AsNoTracking()
            .Where(a => !a.IsDeleted)
            .OrderByDescending(a => a.CreatedAt)
            .ToListAsync(ct);
    }

    public async Task<AgentRecord?> GetAsync(Guid id, CancellationToken ct = default)
    {
        return await _db.Agents.FirstOrDefaultAsync(a => a.Id == id && !a.IsDeleted, ct);
    }

    public async Task AddAsync(AgentRecord record, CancellationToken ct = default)
    {
        await _db.Agents.AddAsync(record, ct);
        await _db.SaveChangesAsync(ct);
    }

    public async Task UpdateAsync(AgentRecord record, CancellationToken ct = default)
    {
        _db.Agents.Update(record);
        await _db.SaveChangesAsync(ct);
    }

    public async Task<bool> SoftDeleteAsync(Guid id, CancellationToken ct = default)
    {
        var record = await _db.Agents.FirstOrDefaultAsync(a => a.Id == id, ct);
        if (record is null || record.IsDeleted)
        {
            return false;
        }

        record.IsDeleted = true;
        await _db.SaveChangesAsync(ct);
        return true;
    }
}
