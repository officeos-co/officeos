namespace EnterpriseAgentOs.Infrastructure.Persistence;

internal sealed class BrowserSessionRepository : IBrowserSessionRepository
{
    private readonly EaosDbContext _eaosDbContext;

    public BrowserSessionRepository(EaosDbContext db)
    {
        _eaosDbContext = db;
    }

    public async Task<BrowserSessionRecord?> GetByAgentAsync(Guid agentId, CancellationToken ct = default)
    {
        return await _eaosDbContext.BrowserSessions
            .FirstOrDefaultAsync(s => s.AgentId == agentId, ct);
    }

    public async Task<BrowserSessionRecord> UpsertAsync(Guid agentId, string runtimeSessionId, string? cookiesJson, CancellationToken ct = default)
    {
        var existing = await _eaosDbContext.BrowserSessions
            .FirstOrDefaultAsync(s => s.AgentId == agentId, ct);

        if (existing is not null)
        {
            existing.RuntimeSessionId = runtimeSessionId;
            existing.CookiesJson = cookiesJson;
            existing.LastAccessedAt = DateTime.UtcNow;
        }
        else
        {
            existing = new BrowserSessionRecord
            {
                AgentId = agentId,
                RuntimeSessionId = runtimeSessionId,
                CookiesJson = cookiesJson,
            };
            _eaosDbContext.BrowserSessions.Add(existing);
        }

        await _eaosDbContext.SaveChangesAsync(ct);
        return existing;
    }

    public async Task DeleteByAgentAsync(Guid agentId, CancellationToken ct = default)
    {
        var existing = await _eaosDbContext.BrowserSessions
            .FirstOrDefaultAsync(s => s.AgentId == agentId, ct);
        if (existing is not null)
        {
            _eaosDbContext.BrowserSessions.Remove(existing);
            await _eaosDbContext.SaveChangesAsync(ct);
        }
    }
}
