namespace EnterpriseAgentOs.Infrastructure.Auth;

internal sealed class SessionRepository : ISessionRepository
{
    private readonly EaosDbContext _eaosDbContext;

    public SessionRepository(EaosDbContext db) => _eaosDbContext = db;

    public async Task<SessionRecord> CreateAsync(Guid userId, string tokenHash, DateTime expiresAt, CancellationToken ct)
    {
        var session = new SessionRecord
        {
            UserId = userId,
            TokenHash = tokenHash,
            ExpiresAt = expiresAt,
        };
        _eaosDbContext.Sessions.Add(session);
        await _eaosDbContext.SaveChangesAsync(ct);
        return session;
    }

    public async Task<SessionRecord?> GetByTokenHashAsync(string tokenHash, CancellationToken ct)
        => await _eaosDbContext.Sessions.Include(s => s.User).FirstOrDefaultAsync(s => s.TokenHash == tokenHash, ct);

    public async Task DeleteAsync(string tokenHash, CancellationToken ct)
    {
        var session = await _eaosDbContext.Sessions.FirstOrDefaultAsync(s => s.TokenHash == tokenHash, ct);
        if (session is not null)
        {
            _eaosDbContext.Sessions.Remove(session);
            await _eaosDbContext.SaveChangesAsync(ct);
        }
    }

    public async Task PurgeExpiredAsync(CancellationToken ct)
    {
        var expired = await _eaosDbContext.Sessions.Where(s => s.ExpiresAt < DateTime.UtcNow).ToListAsync(ct);
        _eaosDbContext.Sessions.RemoveRange(expired);
        await _eaosDbContext.SaveChangesAsync(ct);
    }

    public async Task DeleteByUserIdAsync(Guid userId, CancellationToken ct = default)
    {
        await _eaosDbContext.Sessions.Where(s => s.UserId == userId).ExecuteDeleteAsync(ct);
    }
}
