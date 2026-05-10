namespace OffceOs.Domain.Features.Billing;

public sealed record CreditBudgetResult(long Remaining, bool OverBudget);
