namespace EnterpriseAgentOs.Infrastructure.Features.Management;

internal sealed class SessionRepository : ISessionRepository
{
    private readonly EaosDbContext _eaosDbContext;

    public SessionRepository(EaosDbContext db) => _eaosDbContext = db;

    public async Task<SessionRecord> CreateAsync(Guid userId, string tokenHash, DateTime expiresAt, CancellationToken ct)
    {
        var entity = new SessionEntity
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            TokenHash = tokenHash,
            ExpiresAt = expiresAt,
            CreatedAt = DateTime.UtcNow,
        };
        _eaosDbContext.Sessions.Add(entity);
        await _eaosDbContext.SaveChangesAsync(ct);
        return ToSessionRecord(entity);
    }

    public async Task<SessionRecord?> GetByTokenHashAsync(string tokenHash, CancellationToken ct)
    {
        var entity = await _eaosDbContext.Sessions
            .Include(s => s.User)
            .FirstOrDefaultAsync(s => s.TokenHash == tokenHash, ct);
        return entity is null ? null : ToSessionRecord(entity);
    }

    public async Task DeleteAsync(string tokenHash, CancellationToken ct)
    {
        var entity = await _eaosDbContext.Sessions.FirstOrDefaultAsync(s => s.TokenHash == tokenHash, ct);
        if (entity is not null)
        {
            _eaosDbContext.Sessions.Remove(entity);
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

    // ── Mapping ──────────────────────────────────────────────────────

    private static SessionRecord ToSessionRecord(SessionEntity e) => new()
    {
        Id = e.Id,
        UserId = e.UserId,
        TokenHash = e.TokenHash,
        ExpiresAt = e.ExpiresAt,
        CreatedAt = e.CreatedAt,
        User = e.User is not null ? UserRepository.ToUserRecord(e.User) : null,
    };

    private static SessionEntity ToSessionEntity(SessionRecord r) => new()
    {
        Id = r.Id,
        UserId = r.UserId,
        TokenHash = r.TokenHash,
        ExpiresAt = r.ExpiresAt,
        CreatedAt = r.CreatedAt,
    };
}
