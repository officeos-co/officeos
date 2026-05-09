namespace EnterpriseAgentOs.Infrastructure.Common.Entities;

public sealed class UserSubscriptionEntity
{
    public Guid Id { get; set; }
    public Guid UserId { get; set; }
    public string Plan { get; set; } = "free";
    public string BillingCycle { get; set; } = "monthly";
    public string? StripeCustomerId { get; set; }
    public string? StripeSubscriptionId { get; set; }
    public string? StripeOverageItemId { get; set; }
    public int ConcurrentAgentLimit { get; set; }
    public long CreditBudgetPerMonth { get; set; }
    public long CreditsUsedThisMonth { get; set; }
    public DateTime PeriodStart { get; set; }
    public DateTime PeriodEnd { get; set; }
    public bool IsActive { get; set; }
    public bool OverageEnabled { get; set; }
}
