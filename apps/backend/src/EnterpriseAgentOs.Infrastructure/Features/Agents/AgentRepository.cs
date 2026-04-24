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
        return entities.Select(ToAgentRecord).ToList();
    }

    public async Task<AgentRecord?> GetAsync(Guid id, CancellationToken ct = default)
    {
        var entity = await _eaosDbContext.Agents.FirstOrDefaultAsync(a => a.Id == id && !a.IsDeleted, ct);
        return entity is null ? null : ToAgentRecord(entity);
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

    public async Task UpdateStatusAsync(Guid id, string status, CancellationToken ct = default)
    {
        await _eaosDbContext.Agents.Where(a => a.Id == id)
            .ExecuteUpdateAsync(s => s.SetProperty(a => a.Status, status), ct);
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
        return entities.Select(ToAgentRecord).ToList();
    }

    public async Task HardDeleteByOwnerAsync(Guid ownerId, CancellationToken ct = default)
    {
        await _eaosDbContext.Agents.Where(a => a.OwnerId == ownerId).ExecuteDeleteAsync(ct);
    }

    internal static AgentRecord ToAgentRecord(AgentEntity e) => new()
    {
        Id = e.Id,
        Name = e.Name,
        Provider = e.Provider,
        Model = e.Model,
        Status = e.Status,
        PodName = e.PodName,
        ServiceUrl = e.ServiceUrl,
        Prompt = e.Prompt,
        CreatedAt = e.CreatedAt,
        IsDeleted = e.IsDeleted,
        OwnerId = e.OwnerId,
        EncryptedBackendToken = e.EncryptedBackendToken,
    };

    private static AgentEntity ToAgentEntity(AgentRecord r) => new()
    {
        Id = r.Id,
        Name = r.Name,
        Provider = r.Provider,
        Model = r.Model,
        Status = r.Status,
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
        e.Status = r.Status;
        e.PodName = r.PodName;
        e.ServiceUrl = r.ServiceUrl;
        e.Prompt = r.Prompt;
        e.IsDeleted = r.IsDeleted;
        e.OwnerId = r.OwnerId;
        e.EncryptedBackendToken = r.EncryptedBackendToken;
    }
}
