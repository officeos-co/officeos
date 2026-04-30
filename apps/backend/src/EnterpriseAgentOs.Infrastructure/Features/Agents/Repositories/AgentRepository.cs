namespace EnterpriseAgentOs.Infrastructure.Features.Agents;

internal sealed class AgentRepository : IAgentRepository
{
    private readonly EaosDbContext _eaosDbContext;

    public AgentRepository(EaosDbContext db)
    {
        _eaosDbContext = db;
    }

    public async Task<IReadOnlyList<AgentRecord>> ListAsync(CancellationToken ct = default)
    {
        var entities = await _eaosDbContext.Agents
            .AsNoTracking()
            .Where(a => !a.IsDeleted)
            .OrderByDescending(a => a.CreatedAt)
            .ToListAsync(ct);
        return entities.Select(e => ToAgentRecord(e)).ToList();
    }

    public async Task<AgentRecord?> GetAsync(Guid id, CancellationToken ct = default)
    {
        var entity = await _eaosDbContext.Agents.AsNoTracking()
            .FirstOrDefaultAsync(a => a.Id == id && !a.IsDeleted, ct);
        if (entity is null) return null;

        var personalityFiles = await _eaosDbContext.AgentPersonalities.AsNoTracking()
            .Where(p => p.AgentId == id).ToListAsync(ct);
        var memories = await _eaosDbContext.AgentMemories.AsNoTracking()
            .Where(m => m.AgentId == id).ToListAsync(ct);
        var channelBindings = await _eaosDbContext.AgentChannelBindings.AsNoTracking()
            .Where(b => b.AgentId == id).ToListAsync(ct);
        var session = await _eaosDbContext.AgentSessions.AsNoTracking()
            .Where(s => s.AgentId == id && s.Status == SessionStatus.Active.ToStorageString())
            .OrderByDescending(s => s.CreatedAt).FirstOrDefaultAsync(ct);

        return ToAgentRecord(entity,
            personalityFiles: personalityFiles.Select(ToAgentPersonalityRecord).ToList(),
            memories: memories.Select(ToAgentMemoryRecord).ToList(),
            channelBindings: channelBindings.Select(ToAgentChannelBindingRecord).ToList(),
            activeSession: session is null ? null : ToAgentSessionRecord(session));
    }

    public async Task AddAsync(AgentRecord record, CancellationToken ct = default)
    {
        _eaosDbContext.Agents.Add(ToAgentEntity(record));
        await _eaosDbContext.SaveChangesAsync(ct);
    }

    public async Task UpdateAsync(AgentRecord record, CancellationToken ct = default)
    {
        var entity = await _eaosDbContext.Agents.FirstOrDefaultAsync(a => a.Id == record.Id, ct);
        if (entity is null) return;
        MapToAgentEntity(record, entity);
        await _eaosDbContext.SaveChangesAsync(ct);
    }

    public async Task UpdateStatusAsync(Guid id, AgentStatus status, CancellationToken ct = default)
    {
        var statusStr = status.ToStorageString();
        await _eaosDbContext.Agents.Where(a => a.Id == id)
            .ExecuteUpdateAsync(s => s.SetProperty(a => a.Status, statusStr), ct);
    }

    public async Task<bool> SoftDeleteAsync(Guid id, CancellationToken ct = default)
    {
        var entity = await _eaosDbContext.Agents.FirstOrDefaultAsync(a => a.Id == id, ct);
        if (entity is null || entity.IsDeleted)
        {
            return false;
        }

        entity.IsDeleted = true;
        await _eaosDbContext.SaveChangesAsync(ct);
        return true;
    }

    public async Task<IReadOnlyList<AgentRecord>> ListByOwnerAsync(Guid ownerId, bool includeDeleted = false, CancellationToken ct = default)
    {
        var q = _eaosDbContext.Agents.AsNoTracking().Where(a => a.OwnerId == ownerId);
        if (!includeDeleted) q = q.Where(a => !a.IsDeleted);
        var entities = await q.OrderByDescending(a => a.CreatedAt).ToListAsync(ct);
        return entities.Select(e => ToAgentRecord(e)).ToList();
    }

    public async Task HardDeleteByOwnerAsync(Guid ownerId, CancellationToken ct = default)
    {
        await _eaosDbContext.Agents.Where(a => a.OwnerId == ownerId).ExecuteDeleteAsync(ct);
    }

    // ── Mapping: child records ──────────────────────────────────────

    private static AgentPersonalityRecord ToAgentPersonalityRecord(AgentPersonalityEntity e) => new()
    {
        Id = e.Id, AgentId = e.AgentId, FileName = e.FileName, Content = e.Content,
        CreatedAt = e.CreatedAt, UpdatedAt = e.UpdatedAt,
    };

    private static AgentMemoryRecord ToAgentMemoryRecord(AgentMemoryEntity e) => new()
    {
        Id = e.Id, AgentId = e.AgentId, Key = e.Key, Content = e.Content,
        CreatedAt = e.CreatedAt, UpdatedAt = e.UpdatedAt,
    };

    private static AgentSessionRecord ToAgentSessionRecord(AgentSessionEntity e) => new()
    {
        Id = e.Id, AgentId = e.AgentId, Status = e.Status.ToSessionStatus(), MessageCount = e.MessageCount,
        LastActivityAt = e.LastActivityAt, CreatedAt = e.CreatedAt, EndedAt = e.EndedAt,
    };

    private static AgentChannelBindingRecord ToAgentChannelBindingRecord(AgentChannelBindingEntity e) => new()
    {
        Id = e.Id, AgentId = e.AgentId, ChannelConnectionId = e.ChannelConnectionId,
        Enabled = e.Enabled, Config = e.Config,
    };

    // ── Mapping: agent ──────────────────────────────────────────────

    internal static AgentRecord ToAgentRecord(
        AgentEntity e,
        IReadOnlyList<AgentPersonalityRecord>? personalityFiles = null,
        IReadOnlyList<AgentMemoryRecord>? memories = null,
        IReadOnlyList<AgentCronJobRecord>? cronJobs = null,
        IReadOnlyList<AgentChannelBindingRecord>? channelBindings = null,
        AgentSessionRecord? activeSession = null) => new()
    {
        Id = e.Id,
        Name = e.Name,
        Provider = e.Provider,
        Model = e.Model,
        Status = e.Status.ToAgentStatus(),
        PodName = e.PodName,
        ServiceUrl = e.ServiceUrl,
        Prompt = e.Prompt,
        CreatedAt = e.CreatedAt,
        IsDeleted = e.IsDeleted,
        OwnerId = e.OwnerId,
        EncryptedBackendToken = e.EncryptedBackendToken,
        PersonalityFiles = personalityFiles ?? [],
        Memories = memories ?? [],
        CronJobs = cronJobs ?? [],
        ChannelBindings = channelBindings ?? [],
        ActiveSession = activeSession,
    };

    private static AgentEntity ToAgentEntity(AgentRecord r) => new()
    {
        Id = r.Id,
        Name = r.Name,
        Provider = r.Provider,
        Model = r.Model,
        Status = r.Status.ToStorageString(),
        PodName = r.PodName,
        ServiceUrl = r.ServiceUrl,
        Prompt = r.Prompt,
        CreatedAt = r.CreatedAt,
        IsDeleted = r.IsDeleted,
        OwnerId = r.OwnerId,
        EncryptedBackendToken = r.EncryptedBackendToken,
    };

    private static void MapToAgentEntity(AgentRecord r, AgentEntity e)
    {
        e.Name = r.Name;
        e.Provider = r.Provider;
        e.Model = r.Model;
        e.Status = r.Status.ToStorageString();
        e.PodName = r.PodName;
        e.ServiceUrl = r.ServiceUrl;
        e.Prompt = r.Prompt;
        e.IsDeleted = r.IsDeleted;
        e.OwnerId = r.OwnerId;
        e.EncryptedBackendToken = r.EncryptedBackendToken;
    }
}
