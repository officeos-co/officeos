namespace EnterpriseAgentOs.Infrastructure.Features.Agents;

internal sealed class BrowserSessionRepository : IBrowserSessionRepository
{
    private readonly EaosDbContext _eaosDbContext;

    public BrowserSessionRepository(EaosDbContext db)
    {
        _eaosDbContext = db;
    }

    public async Task<BrowserSessionRecord?> GetByAsync(BrowserSessionFilter filter, CancellationToken ct = default)
    {
        var query = _eaosDbContext.BrowserSessions.AsQueryable();

        if (filter.Id.HasValue)
            query = query.Where(s => s.Id == filter.Id.Value);

        if (filter.AgentId.HasValue)
            query = query.Where(s => s.AgentId == filter.AgentId.Value);

        if (!string.IsNullOrEmpty(filter.RuntimeSessionId))
            query = query.Where(s => s.RuntimeSessionId == filter.RuntimeSessionId);

        var entity = await query.FirstOrDefaultAsync(ct);
        return entity is null ? null : ToBrowserSessionRecord(entity);
    }

    public async Task<BrowserSessionRecord> UpsertAsync(Guid agentId, string runtimeSessionId, string? cookiesJson, CancellationToken ct = default)
    {
        var entity = await _eaosDbContext.BrowserSessions
            .FirstOrDefaultAsync(s => s.AgentId == agentId, ct);

        if (entity is not null)
        {
            entity.RuntimeSessionId = runtimeSessionId;
            entity.CookiesJson = cookiesJson;
            entity.LastAccessedAt = DateTime.UtcNow;
        }
        else
        {
            entity = new BrowserSessionEntity
            {
                Id = Guid.NewGuid(),
                AgentId = agentId,
                RuntimeSessionId = runtimeSessionId,
                CookiesJson = cookiesJson,
                CreatedAt = DateTime.UtcNow,
                LastAccessedAt = DateTime.UtcNow,
            };
            _eaosDbContext.BrowserSessions.Add(entity);
        }

        await _eaosDbContext.SaveChangesAsync(ct);
        return ToBrowserSessionRecord(entity);
    }

    public async Task DeleteByAgentAsync(Guid agentId, CancellationToken ct = default)
    {
        var entity = await _eaosDbContext.BrowserSessions
            .FirstOrDefaultAsync(s => s.AgentId == agentId, ct);
        if (entity is not null)
        {
            _eaosDbContext.BrowserSessions.Remove(entity);
            await _eaosDbContext.SaveChangesAsync(ct);
        }
    }

    // ── Mapping ──────────────────────────────────────────────────────

    private static BrowserSessionRecord ToBrowserSessionRecord(BrowserSessionEntity e) => new()
    {
        Id = e.Id,
        AgentId = e.AgentId,
        RuntimeSessionId = e.RuntimeSessionId,
        CookiesJson = e.CookiesJson,
        CreatedAt = e.CreatedAt,
        LastAccessedAt = e.LastAccessedAt,
    };

    private static BrowserSessionEntity ToBrowserSessionEntity(BrowserSessionRecord r) => new()
    {
        Id = r.Id,
        AgentId = r.AgentId,
        RuntimeSessionId = r.RuntimeSessionId,
        CookiesJson = r.CookiesJson,
        CreatedAt = r.CreatedAt,
        LastAccessedAt = r.LastAccessedAt,
    };
}
