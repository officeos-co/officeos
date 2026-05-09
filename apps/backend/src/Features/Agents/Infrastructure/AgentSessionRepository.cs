namespace OffceOs.Infrastructure.Features.Agents;

internal sealed class AgentSessionRepository : IAgentSessionRepository
{
    private readonly EaosDbContext _eaosDbContext;
    private readonly Dictionary<Guid, AgentSessionRecord> _tracked = new();

    public AgentSessionRepository(EaosDbContext db) => _eaosDbContext = db;

    public async Task<AgentSessionRecord?> GetByAsync(AgentSessionFilter filter, CancellationToken ct = default)
    {
        var query = _eaosDbContext.AgentSessions.AsQueryable();

        if (filter.Id.HasValue)
            query = query.Where(s => s.Id == filter.Id.Value);

        if (filter.AgentId.HasValue)
            query = query.Where(s => s.AgentId == filter.AgentId.Value);

        if (filter.Status.HasValue)
        {
            var status = filter.Status.Value.ToStorageString();
            query = query.Where(s => s.Status == status);
        }

        var entity = await query.OrderByDescending(s => s.CreatedAt).FirstOrDefaultAsync(ct);
        if (entity is null) return null;
        var record = ToAgentSessionRecord(entity);
        _tracked[record.Id] = record;
        return record;
    }

    public async Task<IReadOnlyList<AgentSessionRecord>> ListByAgentAsync(Guid agentId, int limit = 20, CancellationToken ct = default)
    {
        var entities = await _eaosDbContext.AgentSessions
            .Where(s => s.AgentId == agentId)
            .OrderByDescending(s => s.CreatedAt)
            .Take(limit)
            .ToListAsync(ct);
        return entities.Select(ToAgentSessionRecord).ToList();
    }

    public async Task<AgentSessionRecord> CreateAsync(AgentSessionRecord session, CancellationToken ct = default)
    {
        _eaosDbContext.AgentSessions.Add(ToAgentSessionEntity(session));
        await _eaosDbContext.SaveChangesAsync(ct);
        _tracked[session.Id] = session;
        return session;
    }

    public async Task SaveChangesAsync(CancellationToken ct = default)
    {
        foreach (var record in _tracked.Values)
        {
            var entity = await _eaosDbContext.AgentSessions.FindAsync(new object[] { record.Id }, ct);
            if (entity is null) continue;
            MapToAgentSessionEntity(record, entity);
        }
        await _eaosDbContext.SaveChangesAsync(ct);
    }

    public async Task<int> CountByAgentAsync(Guid agentId, CancellationToken ct = default)
        => await _eaosDbContext.AgentSessions.CountAsync(s => s.AgentId == agentId, ct);

    private static AgentSessionRecord ToAgentSessionRecord(AgentSessionEntity e) => new()
    {
        Id = e.Id,
        AgentId = e.AgentId,
        Status = e.Status.ToSessionStatus(),
        MessageCount = e.MessageCount,
        LastActivityAt = e.LastActivityAt,
        CreatedAt = e.CreatedAt,
        EndedAt = e.EndedAt,
        Agent = null,
    };

    private static AgentSessionEntity ToAgentSessionEntity(AgentSessionRecord r) => new()
    {
        Id = r.Id,
        AgentId = r.AgentId,
        Status = r.Status.ToStorageString(),
        MessageCount = r.MessageCount,
        LastActivityAt = r.LastActivityAt,
        CreatedAt = r.CreatedAt,
        EndedAt = r.EndedAt,
    };

    private static void MapToAgentSessionEntity(AgentSessionRecord r, AgentSessionEntity e)
    {
        e.Status = r.Status.ToStorageString();
        e.MessageCount = r.MessageCount;
        e.LastActivityAt = r.LastActivityAt;
        e.EndedAt = r.EndedAt;
    }
}
