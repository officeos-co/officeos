namespace EnterpriseAgentOs.Infrastructure.Persistence.Repositories;

public sealed class AgentMemoryRepository : IAgentMemoryRepository
{
    private readonly EaosDbContext _db;

    public AgentMemoryRepository(EaosDbContext db) => _db = db;

    public async Task<AgentMemoryRecord?> GetAsync(Guid agentId, string key, CancellationToken ct = default)
        => await _db.AgentMemories
            .FirstOrDefaultAsync(m => m.AgentId == agentId && m.Key == key, ct);

    public async Task<IReadOnlyList<AgentMemoryRecord>> ListAsync(Guid agentId, CancellationToken ct = default)
        => await _db.AgentMemories
            .Where(m => m.AgentId == agentId)
            .OrderBy(m => m.Key)
            .ToListAsync(ct);

    public async Task UpsertAsync(Guid agentId, string key, string content, CancellationToken ct = default)
    {
        var existing = await _db.AgentMemories
            .FirstOrDefaultAsync(m => m.AgentId == agentId && m.Key == key, ct);

        if (existing is not null)
        {
            existing.UpdateContent(content);
        }
        else
        {
            _db.AgentMemories.Add(AgentMemoryRecord.Create(agentId, key, content));
        }

        await _db.SaveChangesAsync(ct);
    }

    public async Task<bool> DeleteAsync(Guid agentId, string key, CancellationToken ct = default)
    {
        var record = await _db.AgentMemories
            .FirstOrDefaultAsync(m => m.AgentId == agentId && m.Key == key, ct);
        if (record is null) return false;
        _db.AgentMemories.Remove(record);
        await _db.SaveChangesAsync(ct);
        return true;
    }
}
