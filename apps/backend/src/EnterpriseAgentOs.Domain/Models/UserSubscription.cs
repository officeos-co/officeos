namespace EnterpriseAgentOs.Domain.Models;

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
}
