namespace EnterpriseAgentOs.Api.Entities.Billing;

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

    /// <summary>1 for Free, 3 for Pro.</summary>
    public int ConcurrentAgentLimit { get; set; } = 1;

    /// <summary>2_000_000 for Free, 10_000_000 for Pro.</summary>
    public long TokenBudgetPerMonth { get; set; } = 2_000_000L;

    public long TokensUsedThisMonth { get; set; } = 0;
    public DateTime PeriodStart { get; set; }
    public DateTime PeriodEnd { get; set; }
    public bool IsActive { get; set; } = true;
}
