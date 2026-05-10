namespace OffceOs.Domain.Features.Billing;

public interface IUserBillingService
{
    Task<UserSubscriptionRecord> GetSubscriptionAsync(Guid userId, CancellationToken ct = default);
    Task<CreditBudgetResult> CheckCreditBudgetAsync(Guid userId, CancellationToken ct = default);
    Task<string> CreateCheckoutSessionAsync(Guid userId, string email, string plan, string billingCycle, CancellationToken ct = default);
    Task<string> CreatePortalSessionAsync(Guid userId, string email, CancellationToken ct = default);
    Task CancelSubscriptionAsync(Guid userId, string email, CancellationToken ct = default);
    Task EnableOverageAsync(Guid userId, string email, bool enabled, CancellationToken ct = default);
    Task<IReadOnlyList<InvoiceRecord>> ListInvoicesAsync(
        Guid userId,
        CancellationToken ct = default);
    Task<IReadOnlyDictionary<string, (long MonthlyAmountCents, long YearlyAmountCents, string Currency)>> GetPlanPricesAsync(CancellationToken ct = default);
}

public sealed class BillingProviderException : Exception
{
    public BillingProviderException(string message) : base(message) { }
    public BillingProviderException(string message, Exception innerException) : base(message, innerException) { }
}
