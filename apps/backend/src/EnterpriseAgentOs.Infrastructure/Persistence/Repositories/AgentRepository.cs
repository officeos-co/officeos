namespace EnterpriseAgentOs.Infrastructure.Persistence.Repositories;

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

    public async Task<IReadOnlyList<AgentRecord>> ListByOwnerAsync(Guid ownerId, bool includeDeleted = false, CancellationToken ct = default)
    {
        var q = _db.Agents.AsNoTracking().Where(a => a.OwnerId == ownerId);
        if (!includeDeleted) q = q.Where(a => !a.IsDeleted);
        return await q.OrderByDescending(a => a.CreatedAt).ToListAsync(ct);
    }

    public async Task HardDeleteByOwnerAsync(Guid ownerId, CancellationToken ct = default)
    {
        await _db.Agents.Where(a => a.OwnerId == ownerId).ExecuteDeleteAsync(ct);
    }
}
