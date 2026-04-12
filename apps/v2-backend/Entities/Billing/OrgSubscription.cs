namespace EnterpriseAgentOs.Api.Entities.Billing;

public sealed class OrgSubscription
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string OrganizationId { get; set; } = string.Empty;

    /// <summary>"free", "team", or "enterprise"</summary>
    public string Plan { get; set; } = "free";

    public string? StripeCustomerId { get; set; }
    public string? StripeSubscriptionId { get; set; }

    /// <summary>1 for Free, 10 for Team, custom for Enterprise.</summary>
    public int ConcurrentAgentLimit { get; set; } = 1;

    /// <summary>2_000_000 for Free, 25_000_000 for Team, custom for Enterprise.</summary>
    public long TokenBudgetPerMonth { get; set; } = 2_000_000;

    public long TokensUsedThisMonth { get; set; } = 0;
    public DateTime PeriodStart { get; set; }
    public DateTime PeriodEnd { get; set; }
    public bool IsActive { get; set; } = true;
}
