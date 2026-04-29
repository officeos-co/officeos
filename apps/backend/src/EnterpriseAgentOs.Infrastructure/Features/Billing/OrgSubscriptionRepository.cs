namespace EnterpriseAgentOs.Infrastructure.Features.Billing;

internal sealed class OrgSubscriptionRepository : IOrgSubscriptionRepository
{
    private readonly EaosDbContext _eaosDbContext;

    public OrgSubscriptionRepository(EaosDbContext db)
    {
        _eaosDbContext = db;
    }

    public async Task<OrgSubscription?> GetByOrganizationIdAsync(string organizationId, CancellationToken ct = default)
    {
        var entity = await _eaosDbContext.OrgSubscriptions.FirstOrDefaultAsync(s => s.OrganizationId == organizationId, ct);
        return entity is null ? null : ToOrgSubscription(entity);
    }

    public async Task AddAsync(OrgSubscription sub, CancellationToken ct = default)
    {
        await _eaosDbContext.OrgSubscriptions.AddAsync(ToOrgSubscriptionEntity(sub), ct);
        await _eaosDbContext.SaveChangesAsync(ct);
    }

    public async Task SaveChangesAsync(CancellationToken ct = default)
    {
        await _eaosDbContext.SaveChangesAsync(ct);
    }

    // ── Mapping ──────────────────────────────────────────────────────

    private static OrgSubscription ToOrgSubscription(OrgSubscriptionEntity e) => new()
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

    private static OrgSubscriptionEntity ToOrgSubscriptionEntity(OrgSubscription r) => new()
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
}
