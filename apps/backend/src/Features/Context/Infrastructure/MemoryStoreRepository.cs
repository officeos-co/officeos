namespace EnterpriseAgentOs.Infrastructure.Features.Context;

internal sealed class MemoryStoreRepository : IMemoryStoreRepository
{
    private readonly EaosDbContext _db;

    public MemoryStoreRepository(EaosDbContext db) => _db = db;

    public async Task<IReadOnlyList<MemoryStoreRecord>> ListAsync(Guid ownerId, CancellationToken ct = default)
    {
        var entities = await _db.MemoryStores
            .AsNoTracking()
            .Where(s => s.OwnerId == ownerId)
            .OrderByDescending(s => s.UpdatedAt)
            .ToListAsync(ct);
        return entities.Select(ToRecord).ToList();
    }

    public async Task<MemoryStoreRecord?> GetAsync(Guid id, Guid ownerId, CancellationToken ct = default)
    {
        var entity = await _db.MemoryStores
            .AsNoTracking()
            .FirstOrDefaultAsync(s => s.Id == id && s.OwnerId == ownerId, ct);
        return entity is null ? null : ToRecord(entity);
    }

    public async Task<MemoryStoreRecord> CreateAsync(MemoryStoreRecord store, CancellationToken ct = default)
    {
        var entity = ToEntity(store);
        _db.MemoryStores.Add(entity);
        await _db.SaveChangesAsync(ct);
        return ToRecord(entity);
    }

    public async Task<bool> DeleteAsync(Guid id, Guid ownerId, CancellationToken ct = default)
    {
        var entity = await _db.MemoryStores
            .FirstOrDefaultAsync(s => s.Id == id && s.OwnerId == ownerId, ct);
        if (entity is null) return false;

        await _db.AgentSessionResourceAttachments
            .Where(a => a.ResourceType == AgentResourceKinds.MemoryStore && a.ResourceId == id)
            .ExecuteDeleteAsync(ct);
        _db.MemoryStores.Remove(entity);
        await _db.SaveChangesAsync(ct);
        return true;
    }

    public async Task<IReadOnlyList<MemoryStoreEntryRecord>> ListEntriesAsync(
        Guid memoryStoreId,
        Guid ownerId,
        CancellationToken ct = default)
    {
        var ownsStore = await _db.MemoryStores.AnyAsync(s => s.Id == memoryStoreId && s.OwnerId == ownerId, ct);
        if (!ownsStore) return [];

        return await ListEntriesForStoreAsync(memoryStoreId, ct);
    }

    public async Task<MemoryStoreEntryRecord> UpsertEntryAsync(
        Guid memoryStoreId,
        Guid ownerId,
        string key,
        string content,
        CancellationToken ct = default)
    {
        var ownsStore = await _db.MemoryStores.AnyAsync(s => s.Id == memoryStoreId && s.OwnerId == ownerId, ct);
        if (!ownsStore) throw new InvalidOperationException("Memory store not found.");

        return await UpsertEntryForStoreAsync(memoryStoreId, key, content, ct);
    }

    public async Task<bool> DeleteEntryAsync(Guid memoryStoreId, Guid ownerId, string key, CancellationToken ct = default)
    {
        var ownsStore = await _db.MemoryStores.AnyAsync(s => s.Id == memoryStoreId && s.OwnerId == ownerId, ct);
        if (!ownsStore) return false;

        return await DeleteEntryForStoreAsync(memoryStoreId, key, ct);
    }

    public async Task<IReadOnlyList<MemoryStoreEntryRecord>> ListEntriesForStoreAsync(
        Guid memoryStoreId,
        CancellationToken ct = default)
    {
        var entities = await _db.MemoryStoreEntries
            .AsNoTracking()
            .Where(e => e.MemoryStoreId == memoryStoreId)
            .OrderBy(e => e.Key)
            .ToListAsync(ct);
        return entities.Select(ToRecord).ToList();
    }

    public async Task<MemoryStoreEntryRecord> UpsertEntryForStoreAsync(
        Guid memoryStoreId,
        string key,
        string content,
        CancellationToken ct = default)
    {
        var normalizedKey = key.Trim();
        var entity = await _db.MemoryStoreEntries
            .FirstOrDefaultAsync(e => e.MemoryStoreId == memoryStoreId && e.Key == normalizedKey, ct);
        if (entity is null)
        {
            entity = new MemoryStoreEntryEntity
            {
                Id = Guid.NewGuid(),
                MemoryStoreId = memoryStoreId,
                Key = normalizedKey,
                Content = content,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow,
            };
            _db.MemoryStoreEntries.Add(entity);
        }
        else
        {
            entity.Content = content;
            entity.UpdatedAt = DateTime.UtcNow;
        }

        await _db.SaveChangesAsync(ct);
        return ToRecord(entity);
    }

    public async Task<bool> DeleteEntryForStoreAsync(Guid memoryStoreId, string key, CancellationToken ct = default)
    {
        var normalizedKey = key.Trim();
        var entity = await _db.MemoryStoreEntries
            .FirstOrDefaultAsync(e => e.MemoryStoreId == memoryStoreId && e.Key == normalizedKey, ct);
        if (entity is null) return false;

        _db.MemoryStoreEntries.Remove(entity);
        await _db.SaveChangesAsync(ct);
        return true;
    }

    private static MemoryStoreRecord ToRecord(MemoryStoreEntity e) => new()
    {
        Id = e.Id,
        OwnerId = e.OwnerId,
        DisplayName = e.DisplayName,
        CreatedAt = e.CreatedAt,
        UpdatedAt = e.UpdatedAt,
    };

    private static MemoryStoreEntity ToEntity(MemoryStoreRecord r) => new()
    {
        Id = r.Id,
        OwnerId = r.OwnerId,
        DisplayName = r.DisplayName,
        CreatedAt = r.CreatedAt,
        UpdatedAt = r.UpdatedAt,
    };

    private static MemoryStoreEntryRecord ToRecord(MemoryStoreEntryEntity e) => new()
    {
        Id = e.Id,
        MemoryStoreId = e.MemoryStoreId,
        Key = e.Key,
        Content = e.Content,
        CreatedAt = e.CreatedAt,
        UpdatedAt = e.UpdatedAt,
    };
}
