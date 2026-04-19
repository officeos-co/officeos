namespace EnterpriseAgentOs.Infrastructure.Persistence.Repositories;

public sealed class SessionRepository : ISessionRepository
{
    private readonly EaosDbContext _db;

    public SessionRepository(EaosDbContext db) => _db = db;

    public async Task<SessionRecord> CreateAsync(Guid userId, string tokenHash, DateTime expiresAt, CancellationToken ct)
    {
        var session = new SessionRecord
        {
            UserId = userId,
            TokenHash = tokenHash,
            ExpiresAt = expiresAt,
        };
        _db.Sessions.Add(session);
        await _db.SaveChangesAsync(ct);
        return session;
    }

    public async Task<SessionRecord?> GetByTokenHashAsync(string tokenHash, CancellationToken ct)
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
