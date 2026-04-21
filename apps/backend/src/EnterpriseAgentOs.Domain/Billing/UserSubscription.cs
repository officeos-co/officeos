namespace EnterpriseAgentOs.Domain.Billing;

public sealed class UserSubscription
{
    public Guid Id { get; set; } = Guid.NewGuid();

    /// <summary>FK → UserRecord.Id</summary>
    public Guid UserId { get; set; }

    /// <summary>"free" or "pro"</summary>
    public string Plan { get; set; } = "free";

    /// <summary>"monthly" or "yearly"</summary>
    public string BillingCycle { get; set; } = "monthly";

    public string? StripeCustomerId { get; set; }
    public string? StripeSubscriptionId { get; set; }

    /// <summary>Stripe subscription item ID for the metered overage price, set when OverageEnabled=true.</summary>
    public string? StripeOverageItemId { get; set; }

    /// <summary>1 for Free, 3 for Pro.</summary>
    public int ConcurrentAgentLimit { get; set; } = 1;

    /// <summary>500_000 for Free, 10_000_000 for Pro. Normalized credits (not raw tokens).</summary>
    public long CreditBudgetPerMonth { get; set; } = 500_000L;

    public long CreditsUsedThisMonth { get; set; } = 0;
    public DateTime PeriodStart { get; set; }
    public DateTime PeriodEnd { get; set; }
    public bool IsActive { get; set; } = true;

    /// <summary>When true, usage above CreditBudgetPerMonth is billed via Stripe metered overage.</summary>
    public bool OverageEnabled { get; set; } = false;

    // ── Domain logic ─────────────────────────────────────────────────────────

    /// <summary>Creates a free-tier subscription for a new user.</summary>
    public static UserSubscription CreateDefaultFree(Guid userId)
    {
        var limits = PlanLimits.IndividualFree;
        var now = new DateTime(DateTime.UtcNow.Year, DateTime.UtcNow.Month, 1, 0, 0, 0, DateTimeKind.Utc);
        return new UserSubscription
        {
            UserId = userId,
            Plan = limits.Plan,
            BillingCycle = "monthly",
            ConcurrentAgentLimit = limits.ConcurrentAgents,
            CreditBudgetPerMonth = limits.CreditsPerMonth,
            PeriodStart = now,
            PeriodEnd = now.AddMonths(1),
            IsActive = true,
        };
    }

    /// <summary>Returns remaining credits and whether the budget is exceeded.</summary>
    public (long Remaining, bool OverBudget) CheckBudget()
    {
        var remaining = CreditBudgetPerMonth - CreditsUsedThisMonth;
        return (remaining, remaining < 0);
    }

    /// <summary>Records credit usage against this subscription.</summary>
    public void RecordCredits(long credits)
    {
        CreditsUsedThisMonth += credits;
    }
}
