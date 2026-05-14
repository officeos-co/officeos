namespace OffceOs.Infrastructure.Features.Billing;

internal sealed class OrgSubscriptionRepository : IOrgSubscriptionRepository
{
    private readonly EaosDbContext _eaosDbContext;

    public OrgSubscriptionRepository(EaosDbContext db)
    {
        _eaosDbContext = db;
    }

    public async Task<OrgSubscriptionRecord?> GetByAsync(OrgSubscriptionFilter filter, CancellationToken ct = default)
    {
        var query = _eaosDbContext.OrgSubscriptions.AsQueryable();

        if (filter.Id.HasValue)
            query = query.Where(s => s.Id == filter.Id.Value);

        if (!string.IsNullOrEmpty(filter.OrganizationId))
            query = query.Where(s => s.OrganizationId == filter.OrganizationId);

        if (!string.IsNullOrEmpty(filter.StripeCustomerId))
            query = query.Where(s => s.StripeCustomerId == filter.StripeCustomerId);

        if (!string.IsNullOrEmpty(filter.StripeSubscriptionId))
            query = query.Where(s => s.StripeSubscriptionId == filter.StripeSubscriptionId);

        var entity = await query.FirstOrDefaultAsync(ct);
        return entity is null ? null : ToOrgSubscription(entity);
    }

    public async Task AddAsync(OrgSubscriptionRecord sub, CancellationToken ct = default)
    {
        await _eaosDbContext.OrgSubscriptions.AddAsync(ToOrgSubscriptionEntity(sub), ct);
        await _eaosDbContext.SaveChangesAsync(ct);
    }

    public async Task UpdateAsync(OrgSubscriptionRecord sub, CancellationToken ct = default)
    {
        var entity = await _eaosDbContext.OrgSubscriptions
            .FirstOrDefaultAsync(s => s.OrganizationId == sub.OrganizationId, ct);

        if (entity is null)
            throw new InvalidOperationException($"Organization subscription for org {sub.OrganizationId} was not found.");

        Apply(entity, sub);
        await _eaosDbContext.SaveChangesAsync(ct);
    }

    public async Task SaveChangesAsync(CancellationToken ct = default)
    {
        await _eaosDbContext.SaveChangesAsync(ct);
    }

    // ── Mapping ──────────────────────────────────────────────────────

    private static OrgSubscriptionRecord ToOrgSubscription(OrgSubscriptionEntity e) => new()
    {
        Id = e.Id,
        OrganizationId = e.OrganizationId,
        Plan = e.Plan.ToSubscriptionPlan(),
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

    private static OrgSubscriptionEntity ToOrgSubscriptionEntity(OrgSubscriptionRecord r) => new()
    {
        Id = r.Id,
        OrganizationId = r.OrganizationId,
        Plan = r.Plan.ToStorageString(),
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

    private static void Apply(OrgSubscriptionEntity e, OrgSubscriptionRecord r)
    {
        e.Plan = r.Plan.ToStorageString();
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
