namespace OffceOs.Domain.Features.Billing;

public sealed class UserSubscriptionRecord
{
    public Guid Id { get; init; } = Guid.NewGuid();

    /// <summary>FK → UserRecord.Id</summary>
    public Guid UserId { get; init; }

    public SubscriptionPlan Plan { get; set; } = SubscriptionPlan.Free;
    public BillingCycle BillingCycle { get; set; } = BillingCycle.Monthly;

    public string? StripeCustomerId { get; set; }
    public string? StripeSubscriptionId { get; set; }

    /// <summary>Stripe subscription item ID for the metered overage price, set when OverageEnabled=true.</summary>
    public string? StripeOverageItemId { get; set; }

    /// <summary>1 for Free, 3 for Pro.</summary>
    public int ConcurrentAgentLimit { get; set; } = 1;

    /// <summary>500_000 for Free, 10_000_000 for Pro. Normalized credits (not raw tokens).</summary>
    public long CreditBudgetPerMonth { get; set; } = 500_000L;
    public long CreditsUsedThisMonth { get; set; } = 0;
    public BillingPeriod Period { get; set; }
    public bool IsActive { get; set; } = true;
    public bool OverageEnabled { get; set; } = false;

    // ── Domain logic ─────────────────────────────────────────────────────────

    /// <summary>Creates a free-tier subscription for a new user.</summary>
    public static UserSubscriptionRecord CreateDefaultFree(Guid userId)
    {
        var limits = PlanLimits.IndividualFree;
        var now = new DateTime(DateTime.UtcNow.Year, DateTime.UtcNow.Month, 1, 0, 0, 0, DateTimeKind.Utc);
        return new UserSubscriptionRecord
        {
            UserId = userId,
            Plan = limits.Plan,
            BillingCycle = BillingCycle.Monthly,
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

    /// <summary>Records credit usage against this subscription.</summary>
    public void RecordCredits(long credits)
    {
        CreditsUsedThisMonth += credits;
    }
}
