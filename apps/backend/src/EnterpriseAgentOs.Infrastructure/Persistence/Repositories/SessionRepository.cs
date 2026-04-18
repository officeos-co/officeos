namespace EnterpriseAgentOs.Infrastructure.Persistence.Repositories;

public sealed class SessionRepository : ISessionRepository
{
    private readonly EnterpriseAgentOs.Infrastructure.Persistence.EaosDbContext _db;

    public SessionRepository(EnterpriseAgentOs.Infrastructure.Persistence.EaosDbContext db) => _db = db;

    public async Task<EnterpriseAgentOs.Domain.Models.SessionRecord> CreateAsync(Guid userId, string tokenHash, DateTime expiresAt, CancellationToken ct)
    {
        var session = new EnterpriseAgentOs.Domain.Models.SessionRecord
        {
            UserId = userId,
            TokenHash = tokenHash,
            ExpiresAt = expiresAt,
        };
        _db.Sessions.Add(session);
        await _db.SaveChangesAsync(ct);
        return session;
    }

    public async Task<EnterpriseAgentOs.Domain.Models.SessionRecord?> GetByTokenHashAsync(string tokenHash, CancellationToken ct)
        => await _db.Sessions.Include(s => s.User).FirstOrDefaultAsync(s => s.TokenHash == tokenHash, ct);

    public async Task DeleteAsync(string tokenHash, CancellationToken ct)
    {
        var session = await _db.Sessions.FirstOrDefaultAsync(s => s.TokenHash == tokenHash, ct);
        if (session is not null)
        {
            _db.Sessions.Remove(session);
            await _db.SaveChangesAsync(ct);
        }
    }

    public async Task PurgeExpiredAsync(CancellationToken ct)
    {
        var expired = await _db.Sessions.Where(s => s.ExpiresAt < DateTime.UtcNow).ToListAsync(ct);
        _db.Sessions.RemoveRange(expired);
        await _db.SaveChangesAsync(ct);
    }
}
