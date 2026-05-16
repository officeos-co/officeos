using OffceOs.Database;
using OffceOs.Database.Models;
using OffceOs.Domain.Common.ValueObjects;
using OffceOs.Domain.Features.Agents;
using OffceOs.Domain.Features.Channels;
using OffceOs.Domain.Features.Context;
namespace OffceOs.Infrastructure.Features.Agents;

internal sealed class AgentRepository : IAgentRepository
{
    private readonly EaosDbContext _eaosDbContext;

    public AgentRepository(EaosDbContext db)
    {
        _eaosDbContext = db;
    }

    public async Task<IReadOnlyList<AgentRecord>> ListAsync(AgentFilter filter, CancellationToken ct = default)
    {
        var query = _eaosDbContext.Agents.AsNoTracking().AsQueryable();

        if (filter.Id.HasValue)
            query = query.Where(a => a.Id == filter.Id.Value);

        if (filter.OwnerId.HasValue)
            query = query.Where(a => a.OwnerId == filter.OwnerId.Value);

        if (filter.WorkspaceId.HasValue)
            query = query.Where(a => a.WorkspaceId == filter.WorkspaceId.Value);

        if (!filter.IncludeDeleted)
            query = query.Where(a => !a.IsDeleted);

        var entities = await query.OrderByDescending(a => a.CreatedAt).ToListAsync(ct);
        return entities.Select(e => ToAgentRecord(e)).ToList();
    }

    public async Task<AgentRecord?> GetByAsync(AgentFilter filter, CancellationToken ct = default)
    {
        if (!filter.Id.HasValue && !filter.OwnerId.HasValue)
            throw new ArgumentException("GetByAsync requires at least one AgentFilter selector.", nameof(filter));

        var query = _eaosDbContext.Agents.AsNoTracking().AsQueryable();

        if (filter.Id.HasValue)
            query = query.Where(a => a.Id == filter.Id.Value);

        if (filter.OwnerId.HasValue)
            query = query.Where(a => a.OwnerId == filter.OwnerId.Value);

        if (filter.WorkspaceId.HasValue)
            query = query.Where(a => a.WorkspaceId == filter.WorkspaceId.Value);

        if (!filter.IncludeDeleted)
            query = query.Where(a => !a.IsDeleted);

        var entity = await query.FirstOrDefaultAsync(ct);
        if (entity is null) return null;

        var personalityFiles = await _eaosDbContext.AgentPersonalities.AsNoTracking()
            .Where(p => p.AgentId == entity.Id).ToListAsync(ct);
        var memories = await _eaosDbContext.AgentMemories.AsNoTracking()
            .Where(m => m.AgentId == entity.Id).ToListAsync(ct);
        var channelBindings = await _eaosDbContext.AgentChannelBindings.AsNoTracking()
            .Where(b => b.AgentId == entity.Id).ToListAsync(ct);
        var session = await _eaosDbContext.AgentSessions.AsNoTracking()
            .Where(s => s.AgentId == entity.Id && s.Status == SessionStatus.Active.ToStorageString())
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

    public async Task UpdateStatusAsync(AgentFilter filter, AgentStatus status, CancellationToken ct = default)
    {
        if (!filter.Id.HasValue && !filter.OwnerId.HasValue)
            throw new ArgumentException("UpdateStatusAsync requires at least one AgentFilter selector.", nameof(filter));

        var statusStr = status.ToStorageString();
        var query = _eaosDbContext.Agents.AsQueryable();

        if (filter.Id.HasValue)
            query = query.Where(a => a.Id == filter.Id.Value);

        if (filter.OwnerId.HasValue)
            query = query.Where(a => a.OwnerId == filter.OwnerId.Value);

        if (filter.WorkspaceId.HasValue)
            query = query.Where(a => a.WorkspaceId == filter.WorkspaceId.Value);

        if (!filter.IncludeDeleted)
            query = query.Where(a => !a.IsDeleted);

        await query.ExecuteUpdateAsync(s => s.SetProperty(a => a.Status, statusStr), ct);
    }

    public async Task<bool> SoftDeleteAsync(AgentFilter filter, CancellationToken ct = default)
    {
        if (!filter.Id.HasValue && !filter.OwnerId.HasValue)
            throw new ArgumentException("SoftDeleteAsync requires at least one AgentFilter selector.", nameof(filter));

        var query = _eaosDbContext.Agents.AsQueryable();

        if (filter.Id.HasValue)
            query = query.Where(a => a.Id == filter.Id.Value);

        if (filter.OwnerId.HasValue)
            query = query.Where(a => a.OwnerId == filter.OwnerId.Value);

        if (filter.WorkspaceId.HasValue)
            query = query.Where(a => a.WorkspaceId == filter.WorkspaceId.Value);

        if (!filter.IncludeDeleted)
            query = query.Where(a => !a.IsDeleted);

        var affected = await query.ExecuteUpdateAsync(s => s.SetProperty(a => a.IsDeleted, true), ct);
        return affected > 0;
    }

    public async Task HardDeleteAsync(AgentFilter filter, CancellationToken ct = default)
    {
        if (!filter.Id.HasValue && !filter.OwnerId.HasValue)
            throw new ArgumentException("HardDeleteAsync requires at least one AgentFilter selector.", nameof(filter));

        var query = _eaosDbContext.Agents.AsQueryable();

        if (filter.Id.HasValue)
            query = query.Where(a => a.Id == filter.Id.Value);

        if (filter.OwnerId.HasValue)
            query = query.Where(a => a.OwnerId == filter.OwnerId.Value);

        if (filter.WorkspaceId.HasValue)
            query = query.Where(a => a.WorkspaceId == filter.WorkspaceId.Value);

        if (!filter.IncludeDeleted)
            query = query.Where(a => !a.IsDeleted);

        await query.ExecuteDeleteAsync(ct);
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
        Enabled = e.Enabled, Config = e.Config, CreatedAt = e.CreatedAt,
    };

    // ── Mapping: agent ──────────────────────────────────────────────

    internal static AgentRecord ToAgentRecord(
        AgentEntity e,
        IReadOnlyList<AgentPersonalityRecord>? personalityFiles = null,
        IReadOnlyList<AgentMemoryRecord>? memories = null,
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
        WorkspaceId = e.WorkspaceId,
        EncryptedBackendToken = e.EncryptedBackendToken,
        ActiveDefinitionId = e.ActiveDefinitionId,
        PersonalityFiles = personalityFiles ?? [],
        Memories = memories ?? [],
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
        WorkspaceId = r.WorkspaceId,
        EncryptedBackendToken = r.EncryptedBackendToken,
        ActiveDefinitionId = r.ActiveDefinitionId,
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
        e.WorkspaceId = r.WorkspaceId;
        e.EncryptedBackendToken = r.EncryptedBackendToken;
        e.ActiveDefinitionId = r.ActiveDefinitionId;
    }
}
