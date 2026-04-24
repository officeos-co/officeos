namespace EnterpriseAgentOs.Infrastructure.Features.Billing;

internal sealed class UserSubscriptionRepository : IUserSubscriptionRepository
{
    private readonly EaosDbContext _eaosDbContext;

    public UserSubscriptionRepository(EaosDbContext db)
    {
        _eaosDbContext = db;
    }

    public async Task<UserSubscription?> GetByUserIdAsync(Guid userId, CancellationToken ct = default)
    {
        var entity = await _eaosDbContext.UserSubscriptions.FirstOrDefaultAsync(s => s.UserId == userId, ct);
        return entity is null ? null : ToUserSubscription(entity);
    }

    public async Task AddAsync(UserSubscription sub, CancellationToken ct = default)
    {
        await _eaosDbContext.UserSubscriptions.AddAsync(ToUserSubscriptionEntity(sub), ct);
        await _eaosDbContext.SaveChangesAsync(ct);
    }

    public async Task SaveChangesAsync(CancellationToken ct = default)
    {
        await _eaosDbContext.SaveChangesAsync(ct);
    }

    // ── Mapping ──────────────────────────────────────────────────────

    private static UserSubscription ToUserSubscription(UserSubscriptionEntity e) => new()
    {
        Id = e.Id,
        UserId = e.UserId,
        Plan = e.Plan,
        BillingCycle = e.BillingCycle,
        StripeCustomerId = e.StripeCustomerId,
        StripeSubscriptionId = e.StripeSubscriptionId,
        StripeOverageItemId = e.StripeOverageItemId,
        ConcurrentAgentLimit = e.ConcurrentAgentLimit,
        CreditBudgetPerMonth = e.CreditBudgetPerMonth,
        CreditsUsedThisMonth = e.CreditsUsedThisMonth,
        PeriodStart = e.PeriodStart,
        PeriodEnd = e.PeriodEnd,
        IsActive = e.IsActive,
        OverageEnabled = e.OverageEnabled,
    };

    private static UserSubscriptionEntity ToUserSubscriptionEntity(UserSubscription r) => new()
    {
        Id = r.Id,
        UserId = r.UserId,
        Plan = r.Plan,
        BillingCycle = r.BillingCycle,
        StripeCustomerId = r.StripeCustomerId,
        StripeSubscriptionId = r.StripeSubscriptionId,
        StripeOverageItemId = r.StripeOverageItemId,
        ConcurrentAgentLimit = r.ConcurrentAgentLimit,
        CreditBudgetPerMonth = r.CreditBudgetPerMonth,
        CreditsUsedThisMonth = r.CreditsUsedThisMonth,
        PeriodStart = r.PeriodStart,
        PeriodEnd = r.PeriodEnd,
        IsActive = r.IsActive,
        OverageEnabled = r.OverageEnabled,
    };
}
