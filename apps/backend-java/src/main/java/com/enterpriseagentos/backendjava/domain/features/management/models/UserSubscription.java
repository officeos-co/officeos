package com.enterpriseagentos.backendjava.domain.features.management.models;

import com.enterpriseagentos.backendjava.domain.common.*;
import com.enterpriseagentos.backendjava.domain.common.models.*;
import com.enterpriseagentos.backendjava.domain.common.primitives.*;
import com.enterpriseagentos.backendjava.domain.common.services.*;
import com.enterpriseagentos.backendjava.domain.common.valueobjects.*;
import com.enterpriseagentos.backendjava.domain.events.*;
import com.enterpriseagentos.backendjava.domain.features.agents.dtos.*;
import com.enterpriseagentos.backendjava.domain.features.agents.enums.*;
import com.enterpriseagentos.backendjava.domain.features.agents.interfaces.*;
import com.enterpriseagentos.backendjava.domain.features.agents.models.*;
import com.enterpriseagentos.backendjava.domain.features.agents.registries.*;
import com.enterpriseagentos.backendjava.domain.features.analytics.dtos.*;
import com.enterpriseagentos.backendjava.domain.features.analytics.enums.*;
import com.enterpriseagentos.backendjava.domain.features.analytics.interfaces.*;
import com.enterpriseagentos.backendjava.domain.features.analytics.mappers.*;
import com.enterpriseagentos.backendjava.domain.features.analytics.models.*;
import com.enterpriseagentos.backendjava.domain.features.management.dtos.*;
import com.enterpriseagentos.backendjava.domain.features.management.exceptions.*;
import com.enterpriseagentos.backendjava.domain.features.management.interfaces.*;
import com.enterpriseagentos.backendjava.domain.features.management.models.*;
import com.enterpriseagentos.backendjava.domain.features.management.registries.*;
import com.enterpriseagentos.backendjava.domain.features.management.services.*;
import com.enterpriseagentos.backendjava.domain.features.mcp.dtos.*;
import com.enterpriseagentos.backendjava.domain.features.mcp.enums.*;
import com.enterpriseagentos.backendjava.domain.features.mcp.interfaces.*;
import com.enterpriseagentos.backendjava.domain.features.mcp.models.*;

import java.time.Instant;
import java.time.ZoneOffset;
import java.time.temporal.ChronoUnit;
import java.util.UUID;

public class UserSubscription {
    private UUID id = UUID.randomUUID();
    private UUID userId;
    private SubscriptionPlan plan = SubscriptionPlan.Free;
    private BillingCycle billingCycle = BillingCycle.Monthly;
    private String stripeCustomerId;
    private String stripeSubscriptionId;
    private String stripeOverageItemId;
    private int concurrentAgentLimit = 1;
    private long creditBudgetPerMonth = 500_000L;
    private long creditsUsedThisMonth;
    private BillingPeriod period;
    private boolean isActive = true;
    private boolean overageEnabled;

    public static UserSubscription createDefaultFree(UUID userId) {
        PlanLimit limits = PlanLimits.INDIVIDUAL_FREE;
        Instant start = Instant.now().atOffset(ZoneOffset.UTC)
            .withDayOfMonth(1).truncatedTo(ChronoUnit.DAYS).toInstant();
        UserSubscription subscription = new UserSubscription();
        subscription.userId = userId;
        subscription.plan = limits.plan();
        subscription.billingCycle = BillingCycle.Monthly;
        subscription.concurrentAgentLimit = limits.concurrentAgents();
        subscription.creditBudgetPerMonth = limits.creditsPerMonth();
        subscription.period = new BillingPeriod(start, start.plus(31, ChronoUnit.DAYS));
        return subscription;
    }

    public CreditBudgetResult checkBudget() {
        long remaining = creditBudgetPerMonth - creditsUsedThisMonth;
        return new CreditBudgetResult(remaining, remaining < 0);
    }

    public void recordCredits(long credits) {
        creditsUsedThisMonth += credits;
    }

    public UUID getId() {
        return id;
    }

    public UUID getUserId() {
        return userId;
    }

    public SubscriptionPlan getPlan() {
        return plan;
    }

    public BillingCycle getBillingCycle() {
        return billingCycle;
    }

    public String getStripeCustomerId() {
        return stripeCustomerId;
    }

    public String getStripeSubscriptionId() {
        return stripeSubscriptionId;
    }

    public String getStripeOverageItemId() {
        return stripeOverageItemId;
    }

    public int getConcurrentAgentLimit() {
        return concurrentAgentLimit;
    }

    public long getCreditBudgetPerMonth() {
        return creditBudgetPerMonth;
    }

    public long getCreditsUsedThisMonth() {
        return creditsUsedThisMonth;
    }

    public BillingPeriod getPeriod() {
        return period;
    }

    public boolean getIsActive() {
        return isActive;
    }

    public boolean getOverageEnabled() {
        return overageEnabled;
    }
}
