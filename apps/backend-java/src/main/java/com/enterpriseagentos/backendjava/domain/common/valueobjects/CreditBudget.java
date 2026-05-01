package com.enterpriseagentos.backendjava.domain.common.valueobjects;

import com.enterpriseagentos.backendjava.domain.features.management.dtos.CreditBudgetResult;

public class CreditBudget {
    private long budgetPerMonth;
    private long usedThisMonth;
    private boolean overageEnabled;

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

    public void addCredits(long credits) {
        usedThisMonth += credits;
    }

    public long getBudgetPerMonth() {
        return budgetPerMonth;
    }

    public long getUsedThisMonth() {
        return usedThisMonth;
    }

    public boolean getOverageEnabled() {
        return overageEnabled;
    }
}
