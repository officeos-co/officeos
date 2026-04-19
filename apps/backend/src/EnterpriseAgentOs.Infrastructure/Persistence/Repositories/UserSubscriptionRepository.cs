namespace EnterpriseAgentOs.Infrastructure.Persistence.Repositories;

public sealed class UserSubscriptionRepository : IUserSubscriptionRepository
{
    private readonly EaosDbContext _db;

    public UserSubscriptionRepository(EaosDbContext db)
    {
        _db = db;
    }

    public async Task<UserSubscription?> GetByUserIdAsync(Guid userId, CancellationToken ct = default)
    {
        return await _db.UserSubscriptions.FirstOrDefaultAsync(s => s.UserId == userId, ct);
    }

    public async Task AddAsync(UserSubscription sub, CancellationToken ct = default)
    {
        await _db.UserSubscriptions.AddAsync(sub, ct);
        await _db.SaveChangesAsync(ct);
    }

    public async Task SaveChangesAsync(CancellationToken ct = default)
    {
        await _db.SaveChangesAsync(ct);
    }
}
