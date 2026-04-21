using EnterpriseAgentOs.Domain.Interfaces;
using EnterpriseAgentOs.Domain.Models;
using Microsoft.EntityFrameworkCore;

namespace EnterpriseAgentOs.Infrastructure.Persistence;

internal sealed class AgentSessionRepository : IAgentSessionRepository
{
    private readonly EaosDbContext _db;

    public AgentSessionRepository(EaosDbContext db) => _db = db;

    public async Task<AgentSessionRecord?> GetActiveAsync(Guid agentId, CancellationToken ct = default)
        => await _db.AgentSessions
            .Where(s => s.AgentId == agentId && s.Status == "active")
            .OrderByDescending(s => s.CreatedAt)
            .FirstOrDefaultAsync(ct);

    public async Task<AgentSessionRecord?> GetByIdAsync(Guid sessionId, CancellationToken ct = default)
        => await _db.AgentSessions.FindAsync(new object[] { sessionId }, ct);

    public async Task<IReadOnlyList<AgentSessionRecord>> ListByAgentAsync(Guid agentId, int limit = 20, CancellationToken ct = default)
        => await _db.AgentSessions
            .Where(s => s.AgentId == agentId)
            .OrderByDescending(s => s.CreatedAt)
            .Take(limit)
            .ToListAsync(ct);

    public async Task<AgentSessionRecord> CreateAsync(AgentSessionRecord session, CancellationToken ct = default)
    {
        _db.AgentSessions.Add(session);
        await _db.SaveChangesAsync(ct);
        return session;
    }

    public Task SaveChangesAsync(CancellationToken ct = default)
        => _db.SaveChangesAsync(ct);

    public async Task<int> CountByAgentAsync(Guid agentId, CancellationToken ct = default)
        => await _db.AgentSessions.CountAsync(s => s.AgentId == agentId, ct);
}
