package com.enterpriseagentos.backendjava.domain.common.valueobjects;

import com.enterpriseagentos.backendjava.domain.features.management.CreditBudgetResult;

public class CreditBudget {
    public long budgetPerMonth;
    public long usedThisMonth;
    public boolean overageEnabled;

    public CreditBudget(long budgetPerMonth, long usedThisMonth, boolean overageEnabled) {
        this.budgetPerMonth = budgetPerMonth;
        this.usedThisMonth = usedThisMonth;
        this.overageEnabled = overageEnabled;
    }

    public long remaining() {
        return budgetPerMonth - usedThisMonth;
    }

    public boolean isOverBudget() {
        return remaining() < 0;
    }

    public CreditBudgetResult check() {
        return new CreditBudgetResult(remaining(), isOverBudget());
    }

    public void record(long credits) {
        usedThisMonth += credits;
    }
}
