namespace EnterpriseAgentOs.Api.Billing;

public sealed record UserSubscriptionDto(
    Guid Id,
    Guid UserId,
    string Plan,
    string BillingCycle,
    int ConcurrentAgentLimit,
    long CreditBudgetPerMonth,
    long CreditsUsedThisMonth,
    long CreditsRemaining,
    bool OverBudget,
    bool OverageEnabled,
    DateTime PeriodStart,
    DateTime PeriodEnd,
    bool IsActive);

public sealed record OrgSubscriptionDto(
    Guid Id,
    string OrganizationId,
    string Plan,
    int ConcurrentAgentLimit,
    long CreditBudgetPerMonth,
    long CreditsUsedThisMonth,
    long CreditsRemaining,
    bool OverBudget,
    bool OverageEnabled,
    DateTime PeriodStart,
    DateTime PeriodEnd,
    bool IsActive);

public sealed record PlanLimitDto(
    string Plan,
    int ConcurrentAgents,
    long CreditsPerMonth);

public sealed record PlanLimitsDto(
    PlanLimitDto IndividualFree,
    PlanLimitDto IndividualPro,
    PlanLimitDto OrgFree,
    PlanLimitDto OrgTeam);

public sealed record ModelCostWeightDto(
    string Model,
    int Weight);

public sealed record UsageSummaryDto(
    long CreditsUsedThisMonth,
    long CreditBudgetPerMonth,
    long CreditsRemaining,
    DateTime PeriodStart,
    DateTime PeriodEnd);

/// <summary>Dashboard-facing subscribe response — returns the Stripe checkout URL.</summary>
public sealed record SubscribeResultDto(
    string CheckoutUrl);

/// <summary>
/// Unified billing payload for the dashboard /billing page. Replaces the
/// old "extra usage auto-reload" knob with a simple `extraUsageEnabled`
/// on/off toggle (backed by Stripe metered overage item).
/// </summary>
public sealed record BillingPayload(
    string Plan,
    string PlanDescription,
    string Status,
    string BillingCycle,
    DateTime PeriodStart,
    DateTime PeriodEnd,
    long CreditBudgetPerMonth,
    long CreditsUsedThisMonth,
    long CreditsRemaining,
    bool OverBudget,
    bool ExtraUsageEnabled,
    string? PaymentBrand,
    string? PaymentLast4,
    IReadOnlyList<InvoicePayload> Invoices);
