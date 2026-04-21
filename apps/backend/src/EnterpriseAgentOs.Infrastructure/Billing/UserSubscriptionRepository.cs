namespace EnterpriseAgentOs.Infrastructure.Billing;

internal sealed class UserSubscriptionRepository : IUserSubscriptionRepository
{
    private readonly EaosDbContext _eaosDbContext;

    public UserSubscriptionRepository(EaosDbContext db)
    {
        _eaosDbContext = db;
    }

    public async Task<UserSubscription?> GetByUserIdAsync(Guid userId, CancellationToken ct = default)
    {
        return await _eaosDbContext.UserSubscriptions.FirstOrDefaultAsync(s => s.UserId == userId, ct);
    }

    public async Task AddAsync(UserSubscription sub, CancellationToken ct = default)
    {
        await _eaosDbContext.UserSubscriptions.AddAsync(sub, ct);
        await _eaosDbContext.SaveChangesAsync(ct);
    }

    public async Task SaveChangesAsync(CancellationToken ct = default)
    {
        await _eaosDbContext.SaveChangesAsync(ct);
    }
}
