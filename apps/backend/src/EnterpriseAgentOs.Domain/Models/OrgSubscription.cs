namespace EnterpriseAgentOs.Domain.Models;

public sealed class OrgSubscription
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string OrganizationId { get; set; } = string.Empty;

    /// <summary>"free", "team", or "enterprise"</summary>
    public string Plan { get; set; } = "free";

    public string? StripeCustomerId { get; set; }
    public string? StripeSubscriptionId { get; set; }

    /// <summary>Stripe subscription item ID for the metered overage price, set when OverageEnabled=true.</summary>
    public string? StripeOverageItemId { get; set; }

    /// <summary>1 for Free, 10 for Team, custom for Enterprise.</summary>
    public int ConcurrentAgentLimit { get; set; } = 1;

    /// <summary>500_000 for Free, 25_000_000 for Team, custom for Enterprise. Normalized credits (not raw tokens).</summary>
    public long CreditBudgetPerMonth { get; set; } = 500_000;

    public long CreditsUsedThisMonth { get; set; } = 0;
    public DateTime PeriodStart { get; set; }
    public DateTime PeriodEnd { get; set; }
    public bool IsActive { get; set; } = true;

    /// <summary>When true, usage above CreditBudgetPerMonth is billed via Stripe metered overage.</summary>
    public bool OverageEnabled { get; set; } = false;
}
