package com.enterpriseagentos.backendjava.domain.features.management;

import java.time.Instant;
import java.time.ZoneOffset;
import java.time.temporal.ChronoUnit;
import java.util.UUID;
import com.enterpriseagentos.backendjava.domain.common.valueobjects.BillingPeriod;
import com.enterpriseagentos.backendjava.domain.common.valueobjects.SubscriptionPlan;

public class OrgSubscription {
    public UUID id = UUID.randomUUID();
    public String organizationId;
    public SubscriptionPlan plan = SubscriptionPlan.Free;
    public String stripeCustomerId;
    public String stripeSubscriptionId;
    public String stripeOverageItemId;
    public int concurrentAgentLimit = 1;
    public long creditBudgetPerMonth = 500_000L;
    public long creditsUsedThisMonth;
    public BillingPeriod period;
    public boolean isActive = true;
    public boolean overageEnabled;

    public static OrgSubscription createDefaultFree(String orgId) {
        PlanLimit limits = PlanLimits.ORG_FREE;
        Instant start = Instant.now().atOffset(ZoneOffset.UTC)
            .withDayOfMonth(1).truncatedTo(ChronoUnit.DAYS).toInstant();
        OrgSubscription subscription = new OrgSubscription();
        subscription.organizationId = orgId;
        subscription.plan = limits.plan();
        subscription.concurrentAgentLimit = limits.concurrentAgents();
        subscription.creditBudgetPerMonth = limits.creditsPerMonth();
        subscription.period = new BillingPeriod(start, start.plus(31, ChronoUnit.DAYS));
        return subscription;
    }

    public CreditBudgetResult checkBudget() {
        long remaining = creditBudgetPerMonth - creditsUsedThisMonth;
        return new CreditBudgetResult(remaining, remaining < 0);
    }
}
