namespace OffceOs.Domain.Features.Management;

public sealed class OrgSubscription
{
    public Guid Id { get; init; } = Guid.NewGuid();
    public string OrganizationId { get; init; } = string.Empty;

    public SubscriptionPlan Plan { get; set; } = SubscriptionPlan.Free;

    public string? StripeCustomerId { get; set; }
    public string? StripeSubscriptionId { get; set; }

    /// <summary>Stripe subscription item ID for the metered overage price, set when OverageEnabled=true.</summary>
    public string? StripeOverageItemId { get; set; }

    /// <summary>1 for Free, 10 for Team, custom for Enterprise.</summary>
    public int ConcurrentAgentLimit { get; set; } = 1;

    /// <summary>500_000 for Free, 25_000_000 for Team, custom for Enterprise. Normalized credits (not raw tokens).</summary>
    public long CreditBudgetPerMonth { get; set; } = 500_000;
    public long CreditsUsedThisMonth { get; set; } = 0;
    public BillingPeriod Period { get; set; }
    public bool IsActive { get; set; } = true;
    public bool OverageEnabled { get; set; } = false;

    // ── Domain logic ─────────────────────────────────────────────────────────

    /// <summary>Creates a free-tier subscription for a new organization.</summary>
    public static OrgSubscription CreateDefaultFree(string orgId)
    {
        var limits = PlanLimits.OrgFree;
        var now = new DateTime(DateTime.UtcNow.Year, DateTime.UtcNow.Month, 1, 0, 0, 0, DateTimeKind.Utc);
        return new OrgSubscription
        {
            OrganizationId = orgId,
            Plan = limits.Plan,
            ConcurrentAgentLimit = limits.ConcurrentAgents,
            CreditBudgetPerMonth = limits.CreditsPerMonth,
            Period = new BillingPeriod(now, now.AddMonths(1)),
            IsActive = true,
        };
    }

    /// <summary>Returns remaining credits and whether the budget is exceeded.</summary>
    public CreditBudgetResult CheckBudget()
    {
        var remaining = CreditBudgetPerMonth - CreditsUsedThisMonth;
        return new CreditBudgetResult(remaining, remaining < 0);
    }
}
