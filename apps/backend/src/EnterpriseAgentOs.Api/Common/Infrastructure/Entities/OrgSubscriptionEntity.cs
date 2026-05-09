namespace EnterpriseAgentOs.Infrastructure.Common.Entities;

public sealed class OrgSubscriptionEntity
{
    public Guid Id { get; set; }
    public string OrganizationId { get; set; } = string.Empty;
    public string Plan { get; set; } = "free";
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
