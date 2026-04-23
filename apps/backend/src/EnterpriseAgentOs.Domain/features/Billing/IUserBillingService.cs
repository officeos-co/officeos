namespace EnterpriseAgentOs.Domain.Features.Billing;

public interface IUserBillingService
{
    Task<UserSubscription> GetSubscriptionAsync(Guid userId, CancellationToken ct = default);
    Task<(long Remaining, bool OverBudget)> CheckCreditBudgetAsync(Guid userId, CancellationToken ct = default);
    Task<string> CreateCheckoutSessionAsync(Guid userId, string email, string plan, string billingCycle, CancellationToken ct = default);
    Task<string> CreatePortalSessionAsync(Guid userId, string email, CancellationToken ct = default);
    Task EnableOverageAsync(Guid userId, string email, bool enabled, CancellationToken ct = default);
    Task<IReadOnlyList<InvoicePayload>> ListInvoicesAsync(
        Guid userId,
        CancellationToken ct = default);
}
