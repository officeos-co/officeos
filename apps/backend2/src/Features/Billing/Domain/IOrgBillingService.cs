namespace OffceOs.Domain.Features.Billing;

public interface IOrgBillingService
{
    Task<OrgSubscriptionRecord> GetSubscriptionAsync(string orgId, CancellationToken ct = default);
    Task<CreditBudgetResult> CheckCreditBudgetAsync(string orgId, CancellationToken ct = default);
    Task<string> CreateCustomerAsync(string orgId, string email, CancellationToken ct = default);
    Task<string> CreateSubscriptionAsync(string customerId, string plan, string billingCycle = "monthly", CancellationToken ct = default);
    Task EnableOverageAsync(string orgId, string email, bool enabled, CancellationToken ct = default);
}
