namespace EnterpriseAgentOs.Infrastructure.Features.AgentMemory;

internal sealed class AgentMemoryRepository : IAgentMemoryRepository
{
    private readonly EaosDbContext _eaosDbContext;

    public AgentMemoryRepository(EaosDbContext db) => _eaosDbContext = db;

    public async Task<AgentMemoryRecord?> GetAsync(Guid agentId, string key, CancellationToken ct = default)
    {
        var entity = await _eaosDbContext.AgentMemories
            .FirstOrDefaultAsync(m => m.AgentId == agentId && m.Key == key, ct);
        return entity is null ? null : ToAgentMemoryRecord(entity);
    }

    public async Task<IReadOnlyList<AgentMemoryRecord>> ListAsync(Guid agentId, CancellationToken ct = default)
    {
        var entities = await _eaosDbContext.AgentMemories
            .Where(m => m.AgentId == agentId)
            .OrderBy(m => m.Key)
            .ToListAsync(ct);
        return entities.Select(ToAgentMemoryRecord).ToList();
    }

    public async Task UpsertAsync(Guid agentId, string key, string content, CancellationToken ct = default)
    {
        var existing = await _eaosDbContext.AgentMemories
            .FirstOrDefaultAsync(m => m.AgentId == agentId && m.Key == key, ct);

        if (existing is not null)
        {
            existing.Content = content;
            existing.UpdatedAt = DateTime.UtcNow;
        }
        else
        {
            var record = AgentMemoryRecord.Create(agentId, key, content);
            _eaosDbContext.AgentMemories.Add(ToAgentMemoryEntity(record));
        }

        await _eaosDbContext.SaveChangesAsync(ct);
    }

    public async Task<bool> DeleteAsync(Guid agentId, string key, CancellationToken ct = default)
    {
        var entity = await _eaosDbContext.AgentMemories
            .FirstOrDefaultAsync(m => m.AgentId == agentId && m.Key == key, ct);
        if (entity is null) return false;
        _eaosDbContext.AgentMemories.Remove(entity);
        await _eaosDbContext.SaveChangesAsync(ct);
        return true;
    }

    private static AgentMemoryRecord ToAgentMemoryRecord(AgentMemoryEntity e) => new()
    {
        Id = e.Id,
        AgentId = e.AgentId,
        Key = e.Key,
        Content = e.Content,
        CreatedAt = e.CreatedAt,
        UpdatedAt = e.UpdatedAt,
        Agent = null!,
    };

    private static AgentMemoryEntity ToAgentMemoryEntity(AgentMemoryRecord r) => new()
    {
        Id = r.Id,
        AgentId = r.AgentId,
        Key = r.Key,
        Content = r.Content,
        CreatedAt = r.CreatedAt,
        UpdatedAt = r.UpdatedAt,
    };
}
