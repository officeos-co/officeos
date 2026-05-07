namespace EnterpriseAgentOs.Infrastructure.Features.Agents;

internal sealed class AgentResourceRepository : IAgentResourceRepository
{
    private readonly EaosDbContext _db;

    public AgentResourceRepository(EaosDbContext db) => _db = db;

    public async Task<IReadOnlyList<BrowserResourceRecord>> ListBrowserResourcesAsync(Guid ownerId, CancellationToken ct = default)
    {
        var entities = await _db.BrowserResources
            .AsNoTracking()
            .Where(r => r.OwnerId == ownerId)
            .OrderByDescending(r => r.UpdatedAt)
            .ToListAsync(ct);
        return entities.Select(ToBrowserRecord).ToList();
    }

    public async Task<BrowserResourceRecord?> GetBrowserResourceAsync(Guid id, Guid ownerId, CancellationToken ct = default)
    {
        var entity = await _db.BrowserResources
            .AsNoTracking()
            .FirstOrDefaultAsync(r => r.Id == id && r.OwnerId == ownerId, ct);
        return entity is null ? null : ToBrowserRecord(entity);
    }

    public async Task<BrowserResourceRecord> CreateBrowserResourceAsync(BrowserResourceRecord resource, CancellationToken ct = default)
    {
        var entity = ToBrowserEntity(resource);
        _db.BrowserResources.Add(entity);
        await _db.SaveChangesAsync(ct);
        return ToBrowserRecord(entity);
    }

    public async Task SetBrowserCurrentAgentAsync(Guid browserResourceId, Guid agentId, CancellationToken ct = default)
    {
        var entity = await _db.BrowserResources.FirstOrDefaultAsync(r => r.Id == browserResourceId, ct);
        if (entity is null) return;
        entity.CurrentAgentId = agentId;
        entity.UpdatedAt = DateTime.UtcNow;
        await _db.SaveChangesAsync(ct);
    }

    public async Task<IReadOnlyList<MemoryStoreRecord>> ListMemoryStoresAsync(Guid ownerId, CancellationToken ct = default)
    {
        var entities = await _db.MemoryStores
            .AsNoTracking()
            .Where(s => s.OwnerId == ownerId)
            .OrderByDescending(s => s.UpdatedAt)
            .ToListAsync(ct);
        return entities.Select(ToMemoryStoreRecord).ToList();
    }

    public async Task<MemoryStoreRecord?> GetMemoryStoreAsync(Guid id, Guid ownerId, CancellationToken ct = default)
    {
        var entity = await _db.MemoryStores
            .AsNoTracking()
            .FirstOrDefaultAsync(s => s.Id == id && s.OwnerId == ownerId, ct);
        return entity is null ? null : ToMemoryStoreRecord(entity);
    }

    public async Task<MemoryStoreRecord> CreateMemoryStoreAsync(MemoryStoreRecord store, CancellationToken ct = default)
    {
        var entity = ToMemoryStoreEntity(store);
        _db.MemoryStores.Add(entity);
        await _db.SaveChangesAsync(ct);
        return ToMemoryStoreRecord(entity);
    }

    public async Task<IReadOnlyList<MemoryStoreEntryRecord>> ListMemoryStoreEntriesAsync(Guid memoryStoreId, Guid ownerId, CancellationToken ct = default)
    {
        var ownsStore = await _db.MemoryStores.AnyAsync(s => s.Id == memoryStoreId && s.OwnerId == ownerId, ct);
        if (!ownsStore) return [];

        var entities = await _db.MemoryStoreEntries
            .AsNoTracking()
            .Where(e => e.MemoryStoreId == memoryStoreId)
            .OrderBy(e => e.Key)
            .ToListAsync(ct);
        return entities.Select(ToMemoryEntryRecord).ToList();
    }

    public async Task<MemoryStoreEntryRecord> UpsertMemoryStoreEntryAsync(Guid memoryStoreId, Guid ownerId, string key, string content, CancellationToken ct = default)
    {
        var ownsStore = await _db.MemoryStores.AnyAsync(s => s.Id == memoryStoreId && s.OwnerId == ownerId, ct);
        if (!ownsStore) throw new InvalidOperationException("Memory store not found.");

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
        return ToMemoryEntryRecord(entity);
    }

    public async Task<bool> DeleteMemoryStoreEntryAsync(Guid memoryStoreId, Guid ownerId, string key, CancellationToken ct = default)
    {
        var ownsStore = await _db.MemoryStores.AnyAsync(s => s.Id == memoryStoreId && s.OwnerId == ownerId, ct);
        if (!ownsStore) return false;

        var normalizedKey = key.Trim();
        var entity = await _db.MemoryStoreEntries
            .FirstOrDefaultAsync(e => e.MemoryStoreId == memoryStoreId && e.Key == normalizedKey, ct);
        if (entity is null) return false;

        _db.MemoryStoreEntries.Remove(entity);
        await _db.SaveChangesAsync(ct);
        return true;
    }

    public async Task<IReadOnlyList<MemoryStoreEntryRecord>?> ListActiveMemoryStoreEntriesAsync(Guid agentId, CancellationToken ct = default)
    {
        var attachment = await GetActiveMemoryStoreAttachmentAsync(agentId, ct);
        if (attachment is null) return null;

        var entities = await _db.MemoryStoreEntries
            .AsNoTracking()
            .Where(e => e.MemoryStoreId == attachment.ResourceId)
            .OrderBy(e => e.Key)
            .ToListAsync(ct);
        return entities.Select(ToMemoryEntryRecord).ToList();
    }

    public async Task<MemoryStoreEntryRecord?> UpsertActiveMemoryStoreEntryAsync(Guid agentId, string key, string content, CancellationToken ct = default)
    {
        var attachment = await GetActiveMemoryStoreAttachmentAsync(agentId, ct);
        if (attachment is null) return null;

        var normalizedKey = key.Trim();
        var entity = await _db.MemoryStoreEntries
            .FirstOrDefaultAsync(e => e.MemoryStoreId == attachment.ResourceId && e.Key == normalizedKey, ct);
        if (entity is null)
        {
            entity = new MemoryStoreEntryEntity
            {
                Id = Guid.NewGuid(),
                MemoryStoreId = attachment.ResourceId,
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
        return ToMemoryEntryRecord(entity);
    }

    public async Task<bool?> DeleteActiveMemoryStoreEntryAsync(Guid agentId, string key, CancellationToken ct = default)
    {
        var attachment = await GetActiveMemoryStoreAttachmentAsync(agentId, ct);
        if (attachment is null) return null;

        var normalizedKey = key.Trim();
        var entity = await _db.MemoryStoreEntries
            .FirstOrDefaultAsync(e => e.MemoryStoreId == attachment.ResourceId && e.Key == normalizedKey, ct);
        if (entity is null) return false;

        _db.MemoryStoreEntries.Remove(entity);
        await _db.SaveChangesAsync(ct);
        return true;
    }

    public async Task AttachToSessionAsync(AgentSessionResourceAttachmentRecord attachment, CancellationToken ct = default)
    {
        var existing = await _db.AgentSessionResourceAttachments.FirstOrDefaultAsync(
            a => a.SessionId == attachment.SessionId
                && a.ResourceType == attachment.ResourceType
                && a.ResourceId == attachment.ResourceId,
            ct);

        if (existing is null)
        {
            _db.AgentSessionResourceAttachments.Add(ToAttachmentEntity(attachment));
        }
        else
        {
            existing.AccessMode = attachment.AccessMode;
            existing.Instructions = attachment.Instructions;
        }

        await _db.SaveChangesAsync(ct);
    }

    public async Task<IReadOnlyList<AgentSessionResourceAttachmentRecord>> ListSessionAttachmentsAsync(Guid sessionId, CancellationToken ct = default)
    {
        var entities = await _db.AgentSessionResourceAttachments
            .AsNoTracking()
            .Where(a => a.SessionId == sessionId)
            .OrderBy(a => a.CreatedAt)
            .ToListAsync(ct);
        return entities.Select(ToAttachmentRecord).ToList();
    }

    public async Task<AgentSessionResourceAttachmentRecord?> GetActiveMemoryStoreAttachmentAsync(Guid agentId, CancellationToken ct = default)
    {
        var entity = await _db.AgentSessionResourceAttachments
            .AsNoTracking()
            .Join(
                _db.AgentSessions.AsNoTracking(),
                attachment => attachment.SessionId,
                session => session.Id,
                (attachment, session) => new { attachment, session })
            .Where(x => x.attachment.AgentId == agentId
                && x.attachment.ResourceType == AgentResourceTypes.MemoryStore
                && x.session.Status == SessionStatus.Active.ToStorageString())
            .OrderByDescending(x => x.attachment.CreatedAt)
            .Select(x => x.attachment)
            .FirstOrDefaultAsync(ct);
        return entity is null ? null : ToAttachmentRecord(entity);
    }

    private static BrowserResourceRecord ToBrowserRecord(BrowserResourceEntity e) => new()
    {
        Id = e.Id,
        OwnerId = e.OwnerId,
        DisplayName = e.DisplayName,
        CurrentAgentId = e.CurrentAgentId,
        CreatedAt = e.CreatedAt,
        UpdatedAt = e.UpdatedAt,
    };

    private static BrowserResourceEntity ToBrowserEntity(BrowserResourceRecord r) => new()
    {
        Id = r.Id,
        OwnerId = r.OwnerId,
        DisplayName = r.DisplayName,
        CurrentAgentId = r.CurrentAgentId,
        CreatedAt = r.CreatedAt,
        UpdatedAt = r.UpdatedAt,
    };

    private static MemoryStoreRecord ToMemoryStoreRecord(MemoryStoreEntity e) => new()
    {
        Id = e.Id,
        OwnerId = e.OwnerId,
        DisplayName = e.DisplayName,
        CreatedAt = e.CreatedAt,
        UpdatedAt = e.UpdatedAt,
    };

    private static MemoryStoreEntity ToMemoryStoreEntity(MemoryStoreRecord r) => new()
    {
        Id = r.Id,
        OwnerId = r.OwnerId,
        DisplayName = r.DisplayName,
        CreatedAt = r.CreatedAt,
        UpdatedAt = r.UpdatedAt,
    };

    private static MemoryStoreEntryRecord ToMemoryEntryRecord(MemoryStoreEntryEntity e) => new()
    {
        Id = e.Id,
        MemoryStoreId = e.MemoryStoreId,
        Key = e.Key,
        Content = e.Content,
        CreatedAt = e.CreatedAt,
        UpdatedAt = e.UpdatedAt,
    };

    private static AgentSessionResourceAttachmentRecord ToAttachmentRecord(AgentSessionResourceAttachmentEntity e) => new()
    {
        Id = e.Id,
        AgentId = e.AgentId,
        SessionId = e.SessionId,
        ResourceType = e.ResourceType,
        ResourceId = e.ResourceId,
        AccessMode = e.AccessMode,
        Instructions = e.Instructions,
        CreatedAt = e.CreatedAt,
    };

    private static AgentSessionResourceAttachmentEntity ToAttachmentEntity(AgentSessionResourceAttachmentRecord r) => new()
    {
        Id = r.Id,
        AgentId = r.AgentId,
        SessionId = r.SessionId,
        ResourceType = r.ResourceType,
        ResourceId = r.ResourceId,
        AccessMode = r.AccessMode,
        Instructions = r.Instructions,
        CreatedAt = r.CreatedAt,
    };
}
