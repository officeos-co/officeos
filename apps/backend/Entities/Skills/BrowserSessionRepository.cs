namespace EnterpriseAgentOs.Api.Entities.Skills;

public sealed class BrowserSessionRepository : IBrowserSessionRepository
{
    private readonly EnterpriseAgentOs.Api.Database.EaosDbContext _db;

    public BrowserSessionRepository(EnterpriseAgentOs.Api.Database.EaosDbContext db)
    {
        _db = db;
    }

    public async Task<EnterpriseAgentOs.Api.Database.Models.BrowserSessionRecord?> GetByAgentAsync(Guid agentId, CancellationToken ct = default)
    {
        return await _db.BrowserSessions
            .FirstOrDefaultAsync(s => s.AgentId == agentId, ct);
    }

    public async Task<EnterpriseAgentOs.Api.Database.Models.BrowserSessionRecord> UpsertAsync(Guid agentId, string runtimeSessionId, string? cookiesJson, CancellationToken ct = default)
    {
        var existing = await _db.BrowserSessions
            .FirstOrDefaultAsync(s => s.AgentId == agentId, ct);

        if (existing is not null)
        {
            existing.RuntimeSessionId = runtimeSessionId;
            existing.CookiesJson = cookiesJson;
            existing.LastAccessedAt = DateTime.UtcNow;
        }
        else
        {
            existing = new EnterpriseAgentOs.Api.Database.Models.BrowserSessionRecord
            {
                AgentId = agentId,
                RuntimeSessionId = runtimeSessionId,
                CookiesJson = cookiesJson,
            };
            _db.BrowserSessions.Add(existing);
        }

        await _db.SaveChangesAsync(ct);
        return existing;
    }

    public async Task DeleteByAgentAsync(Guid agentId, CancellationToken ct = default)
    {
        var existing = await _db.BrowserSessions
            .FirstOrDefaultAsync(s => s.AgentId == agentId, ct);
        if (existing is not null)
        {
            _db.BrowserSessions.Remove(existing);
            await _db.SaveChangesAsync(ct);
        }
    }
}
