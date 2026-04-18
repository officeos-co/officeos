namespace EnterpriseAgentOs.Infrastructure.Persistence.Repositories;

public sealed class AgentRepository : IAgentRepository
{
    private readonly EnterpriseAgentOs.Infrastructure.Persistence.EaosDbContext _db;

    public AgentRepository(EnterpriseAgentOs.Infrastructure.Persistence.EaosDbContext db)
    {
        _db = db;
    }

    public async Task<IReadOnlyList<EnterpriseAgentOs.Domain.Models.AgentRecord>> ListAsync(CancellationToken ct = default)
    {
        return await _db.Agents
            .AsNoTracking()
            .Where(a => !a.IsDeleted)
            .OrderByDescending(a => a.CreatedAt)
            .ToListAsync(ct);
    }

    public async Task<EnterpriseAgentOs.Domain.Models.AgentRecord?> GetAsync(Guid id, CancellationToken ct = default)
    {
        return await _db.Agents.FirstOrDefaultAsync(a => a.Id == id && !a.IsDeleted, ct);
    }

    public async Task AddAsync(EnterpriseAgentOs.Domain.Models.AgentRecord record, CancellationToken ct = default)
    {
        await _db.Agents.AddAsync(record, ct);
        await _db.SaveChangesAsync(ct);
    }

    public async Task UpdateAsync(EnterpriseAgentOs.Domain.Models.AgentRecord record, CancellationToken ct = default)
    {
        _db.Agents.Update(record);
        await _db.SaveChangesAsync(ct);
    }

    public async Task UpdateStatusAsync(Guid id, string status, CancellationToken ct = default)
    {
        await _db.Agents.Where(a => a.Id == id)
            .ExecuteUpdateAsync(s => s.SetProperty(a => a.Status, status), ct);
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
