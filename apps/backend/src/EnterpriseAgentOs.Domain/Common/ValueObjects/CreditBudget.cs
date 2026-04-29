namespace EnterpriseAgentOs.Domain.Common.ValueObjects;

/// <summary>
/// Encapsulates credit budget state and logic shared between UserSubscription and OrgSubscription.
/// </summary>
public sealed class CreditBudget
{
    public long BudgetPerMonth { get; set; }
    public long UsedThisMonth { get; set; }
    public bool OverageEnabled { get; set; }

    public long Remaining => BudgetPerMonth - UsedThisMonth;
    public bool IsOverBudget => Remaining < 0;

    public CreditBudget(long budgetPerMonth, long usedThisMonth, bool overageEnabled)
    {
        BudgetPerMonth = budgetPerMonth;
        UsedThisMonth = usedThisMonth;
        OverageEnabled = overageEnabled;
    }

    public CreditBudgetResult Check() => new(Remaining, IsOverBudget);

    public void Record(long credits) => UsedThisMonth += credits;
}
