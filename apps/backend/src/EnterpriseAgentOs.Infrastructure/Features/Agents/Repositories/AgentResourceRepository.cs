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

    public async Task<bool> DeleteBrowserResourceAsync(Guid id, Guid ownerId, CancellationToken ct = default)
    {
        var entity = await _db.BrowserResources
            .FirstOrDefaultAsync(r => r.Id == id && r.OwnerId == ownerId, ct);
        if (entity is null) return false;

        await _db.AgentSessionResourceAttachments
            .Where(a => a.ResourceType == AgentResourceTypes.Browser && a.ResourceId == id)
            .ExecuteDeleteAsync(ct);
        _db.BrowserResources.Remove(entity);
        await _db.SaveChangesAsync(ct);
        return true;
    }

    public async Task SetBrowserCurrentAgentAsync(Guid browserResourceId, Guid agentId, CancellationToken ct = default)
    {
        var entity = await _db.BrowserResources.FirstOrDefaultAsync(r => r.Id == browserResourceId, ct);
        if (entity is null) return;
        entity.CurrentAgentId = agentId;
        entity.UpdatedAt = DateTime.UtcNow;
        await _db.SaveChangesAsync(ct);
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
