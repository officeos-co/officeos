namespace EnterpriseAgentOs.Domain.Features.Management;

public interface IUserBillingService
{
    Task<UserSubscription> GetSubscriptionAsync(Guid userId, CancellationToken ct = default);
    Task<CreditBudgetResult> CheckCreditBudgetAsync(Guid userId, CancellationToken ct = default);
    Task<string> CreateCheckoutSessionAsync(Guid userId, string email, string plan, string billingCycle, CancellationToken ct = default);
    Task<string> CreatePortalSessionAsync(Guid userId, string email, CancellationToken ct = default);
    Task EnableOverageAsync(Guid userId, string email, bool enabled, CancellationToken ct = default);
    Task<IReadOnlyList<InvoicePayload>> ListInvoicesAsync(
        Guid userId,
        CancellationToken ct = default);
    Task<IReadOnlyDictionary<string, (long MonthlyAmountCents, long YearlyAmountCents, string Currency)>> GetPlanPricesAsync(CancellationToken ct = default);
}
