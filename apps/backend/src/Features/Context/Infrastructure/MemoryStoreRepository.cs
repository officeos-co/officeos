using OffceOs.Database;
using OffceOs.Database.Models;
using OffceOs.Features.Context.Domain;
using OffceOs.Features.Agents.Domain;
namespace OffceOs.Features.Context.Infrastructure;

internal sealed class MemoryStoreRepository : IMemoryStoreRepository
{
    private readonly EaosDbContext _eaosDbContext;

    public MemoryStoreRepository(EaosDbContext db) => _eaosDbContext = db;

    public async Task<IReadOnlyList<MemoryStoreRecord>> ListAsync(Guid? ownerId, Guid workspaceId, CancellationToken ct = default)
    {
        var query = _eaosDbContext.MemoryStores
            .AsNoTracking()
            .Where(s => s.WorkspaceId == workspaceId);

        if (ownerId.HasValue)
            query = query.Where(s => s.OwnerId == ownerId.Value);

        var entities = await query
            .OrderByDescending(s => s.UpdatedAt)
            .ToListAsync(ct);
        return entities.Select(ToRecord).ToList();
    }

    public async Task<MemoryStoreRecord?> GetAsync(Guid id, Guid? ownerId, Guid? workspaceId = null, CancellationToken ct = default)
    {
        var query = _eaosDbContext.MemoryStores
            .AsNoTracking()
            .Where(s => s.Id == id);

        if (ownerId.HasValue)
            query = query.Where(s => s.OwnerId == ownerId.Value);

        if (workspaceId.HasValue)
            query = query.Where(s => s.WorkspaceId == workspaceId.Value);

        var entity = await query.FirstOrDefaultAsync(ct);
        return entity is null ? null : ToRecord(entity);
    }

    public async Task<MemoryStoreRecord> CreateAsync(MemoryStoreRecord store, CancellationToken ct = default)
    {
        var entity = ToEntity(store);
        _eaosDbContext.MemoryStores.Add(entity);
        await _eaosDbContext.SaveChangesAsync(ct);
        return ToRecord(entity);
    }

    public async Task<MemoryStoreRecord?> UpdateAsync(Guid id, Guid? ownerId, Guid workspaceId, string displayName, CancellationToken ct = default)
    {
        var query = _eaosDbContext.MemoryStores
            .Where(s => s.Id == id && s.WorkspaceId == workspaceId);

        if (ownerId.HasValue)
            query = query.Where(s => s.OwnerId == ownerId.Value);

        var entity = await query.FirstOrDefaultAsync(ct);
        if (entity is null) return null;

        entity.DisplayName = MemoryStoreRecord.Create(entity.OwnerId, entity.WorkspaceId, displayName).DisplayName;
        entity.UpdatedAt = DateTime.UtcNow;
        await _eaosDbContext.SaveChangesAsync(ct);
        return ToRecord(entity);
    }

    public async Task<bool> DeleteAsync(Guid id, Guid? ownerId, Guid workspaceId, CancellationToken ct = default)
    {
        var query = _eaosDbContext.MemoryStores
            .Where(s => s.Id == id && s.WorkspaceId == workspaceId);

        if (ownerId.HasValue)
            query = query.Where(s => s.OwnerId == ownerId.Value);

        var entity = await query.FirstOrDefaultAsync(ct);
        if (entity is null) return false;

        await _eaosDbContext.AgentSessionResourceAttachments
            .Where(a => a.ResourceType == AgentResourceKinds.MemoryStore && a.ResourceId == id)
            .ExecuteDeleteAsync(ct);
        _eaosDbContext.MemoryStores.Remove(entity);
        await _eaosDbContext.SaveChangesAsync(ct);
        return true;
    }

    public async Task<IReadOnlyList<MemoryStoreEntryRecord>> ListEntriesAsync(
        Guid memoryStoreId,
        Guid? ownerId,
        Guid workspaceId,
        CancellationToken ct = default)
    {
        var ownsStore = await StoreExistsAsync(memoryStoreId, ownerId, workspaceId, ct);
        if (!ownsStore) return [];

        return await ListEntriesForStoreAsync(memoryStoreId, ct);
    }

    public async Task<MemoryStoreEntryRecord> UpsertEntryAsync(
        Guid memoryStoreId,
        Guid? ownerId,
        Guid workspaceId,
        string key,
        string content,
        CancellationToken ct = default)
    {
        var ownsStore = await StoreExistsAsync(memoryStoreId, ownerId, workspaceId, ct);
        if (!ownsStore) throw new InvalidOperationException("Memory store not found.");

        return await UpsertEntryForStoreAsync(memoryStoreId, key, content, ct);
    }

    public async Task<bool> DeleteEntryAsync(Guid memoryStoreId, Guid? ownerId, Guid workspaceId, string key, CancellationToken ct = default)
    {
        var ownsStore = await StoreExistsAsync(memoryStoreId, ownerId, workspaceId, ct);
        if (!ownsStore) return false;

        return await DeleteEntryForStoreAsync(memoryStoreId, key, ct);
    }

    public async Task<IReadOnlyList<MemoryStoreEntryRecord>> ListEntriesForStoreAsync(
        Guid memoryStoreId,
        CancellationToken ct = default)
    {
        var entities = await _eaosDbContext.MemoryStoreEntries
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
        var entity = await _eaosDbContext.MemoryStoreEntries
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
            _eaosDbContext.MemoryStoreEntries.Add(entity);
        }
        else
        {
            entity.Content = content;
            entity.UpdatedAt = DateTime.UtcNow;
        }

        await _eaosDbContext.SaveChangesAsync(ct);
        return ToRecord(entity);
    }

    public async Task<bool> DeleteEntryForStoreAsync(Guid memoryStoreId, string key, CancellationToken ct = default)
    {
        var normalizedKey = key.Trim();
        var entity = await _eaosDbContext.MemoryStoreEntries
            .FirstOrDefaultAsync(e => e.MemoryStoreId == memoryStoreId && e.Key == normalizedKey, ct);
        if (entity is null) return false;

        _eaosDbContext.MemoryStoreEntries.Remove(entity);
        await _eaosDbContext.SaveChangesAsync(ct);
        return true;
    }

    private async Task<bool> StoreExistsAsync(Guid memoryStoreId, Guid? ownerId, Guid workspaceId, CancellationToken ct)
    {
        var query = _eaosDbContext.MemoryStores
            .AsNoTracking()
            .Where(s => s.Id == memoryStoreId && s.WorkspaceId == workspaceId);

        if (ownerId.HasValue)
            query = query.Where(s => s.OwnerId == ownerId.Value);

        return await query.AnyAsync(ct);
    }

    private static MemoryStoreRecord ToRecord(MemoryStoreEntity e) => new()
    {
        Id = e.Id,
        OwnerId = e.OwnerId,
        WorkspaceId = e.WorkspaceId,
        DisplayName = e.DisplayName,
        CreatedAt = e.CreatedAt,
        UpdatedAt = e.UpdatedAt,
    };

    private static MemoryStoreEntity ToEntity(MemoryStoreRecord r) => new()
    {
        Id = r.Id,
        OwnerId = r.OwnerId,
        WorkspaceId = r.WorkspaceId,
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
