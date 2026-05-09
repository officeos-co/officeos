namespace OffceOs.Infrastructure.Features.Management;

internal sealed class UserSubscriptionRepository : IUserSubscriptionRepository
{
    private readonly EaosDbContext _eaosDbContext;

    public UserSubscriptionRepository(EaosDbContext db)
    {
        _eaosDbContext = db;
    }

    public async Task<UserSubscription?> GetByAsync(UserSubscriptionFilter filter, CancellationToken ct = default)
    {
        var query = _eaosDbContext.UserSubscriptions.AsQueryable();

        if (filter.Id.HasValue)
            query = query.Where(s => s.Id == filter.Id.Value);

        if (filter.UserId.HasValue)
            query = query.Where(s => s.UserId == filter.UserId.Value);

        if (!string.IsNullOrEmpty(filter.StripeCustomerId))
            query = query.Where(s => s.StripeCustomerId == filter.StripeCustomerId);

        if (!string.IsNullOrEmpty(filter.StripeSubscriptionId))
            query = query.Where(s => s.StripeSubscriptionId == filter.StripeSubscriptionId);

        var entity = await query.FirstOrDefaultAsync(ct);
        return entity is null ? null : ToUserSubscription(entity);
    }

    public async Task AddAsync(UserSubscription sub, CancellationToken ct = default)
    {
        await _eaosDbContext.UserSubscriptions.AddAsync(ToUserSubscriptionEntity(sub), ct);
        await _eaosDbContext.SaveChangesAsync(ct);
    }

    public async Task UpdateAsync(UserSubscription sub, CancellationToken ct = default)
    {
        var entity = await _eaosDbContext.UserSubscriptions
            .FirstOrDefaultAsync(s => s.UserId == sub.UserId, ct);

        if (entity is null)
            throw new InvalidOperationException($"User subscription for user {sub.UserId} was not found.");

        Apply(entity, sub);
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
        Plan = e.Plan.ToSubscriptionPlan(),
        BillingCycle = e.BillingCycle.ToBillingCycle(),
        StripeCustomerId = e.StripeCustomerId,
        StripeSubscriptionId = e.StripeSubscriptionId,
        StripeOverageItemId = e.StripeOverageItemId,
        ConcurrentAgentLimit = e.ConcurrentAgentLimit,
        CreditBudgetPerMonth = e.CreditBudgetPerMonth,
        CreditsUsedThisMonth = e.CreditsUsedThisMonth,
        Period = new BillingPeriod(e.PeriodStart, e.PeriodEnd),
        IsActive = e.IsActive,
        OverageEnabled = e.OverageEnabled,
    };

    private static UserSubscriptionEntity ToUserSubscriptionEntity(UserSubscription r) => new()
    {
        Id = r.Id,
        UserId = r.UserId,
        Plan = r.Plan.ToStorageString(),
        BillingCycle = r.BillingCycle.ToStorageString(),
        StripeCustomerId = r.StripeCustomerId,
        StripeSubscriptionId = r.StripeSubscriptionId,
        StripeOverageItemId = r.StripeOverageItemId,
        ConcurrentAgentLimit = r.ConcurrentAgentLimit,
        CreditBudgetPerMonth = r.CreditBudgetPerMonth,
        CreditsUsedThisMonth = r.CreditsUsedThisMonth,
        PeriodStart = r.Period.Start,
        PeriodEnd = r.Period.End,
        IsActive = r.IsActive,
        OverageEnabled = r.OverageEnabled,
    };

    private static void Apply(UserSubscriptionEntity e, UserSubscription r)
    {
        e.Plan = r.Plan.ToStorageString();
        e.BillingCycle = r.BillingCycle.ToStorageString();
        e.StripeCustomerId = r.StripeCustomerId;
        e.StripeSubscriptionId = r.StripeSubscriptionId;
        e.StripeOverageItemId = r.StripeOverageItemId;
        e.ConcurrentAgentLimit = r.ConcurrentAgentLimit;
        e.CreditBudgetPerMonth = r.CreditBudgetPerMonth;
        e.CreditsUsedThisMonth = r.CreditsUsedThisMonth;
        e.PeriodStart = r.Period.Start;
        e.PeriodEnd = r.Period.End;
        e.IsActive = r.IsActive;
        e.OverageEnabled = r.OverageEnabled;
    }
}
