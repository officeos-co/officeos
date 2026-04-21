namespace EnterpriseAgentOs.Infrastructure.Persistence;

internal sealed class AgentMemoryRepository : IAgentMemoryRepository
{
    private readonly EaosDbContext _eaosDbContext;

    public AgentMemoryRepository(EaosDbContext db) => _eaosDbContext = db;

    public async Task<AgentMemoryRecord?> GetAsync(Guid agentId, string key, CancellationToken ct = default)
        => await _eaosDbContext.AgentMemories
            .FirstOrDefaultAsync(m => m.AgentId == agentId && m.Key == key, ct);

    public async Task<IReadOnlyList<AgentMemoryRecord>> ListAsync(Guid agentId, CancellationToken ct = default)
        => await _eaosDbContext.AgentMemories
            .Where(m => m.AgentId == agentId)
            .OrderBy(m => m.Key)
            .ToListAsync(ct);

    public async Task UpsertAsync(Guid agentId, string key, string content, CancellationToken ct = default)
    {
        var existing = await _eaosDbContext.AgentMemories
            .FirstOrDefaultAsync(m => m.AgentId == agentId && m.Key == key, ct);

        if (existing is not null)
        {
            existing.UpdateContent(content);
        }
        else
        {
            _eaosDbContext.AgentMemories.Add(AgentMemoryRecord.Create(agentId, key, content));
        }

        await _eaosDbContext.SaveChangesAsync(ct);
    }

    public async Task<bool> DeleteAsync(Guid agentId, string key, CancellationToken ct = default)
    {
        var record = await _eaosDbContext.AgentMemories
            .FirstOrDefaultAsync(m => m.AgentId == agentId && m.Key == key, ct);
        if (record is null) return false;
        _eaosDbContext.AgentMemories.Remove(record);
        await _eaosDbContext.SaveChangesAsync(ct);
        return true;
    }
}
