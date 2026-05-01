package com.enterpriseagentos.backendjava.domain.features.management;

import com.enterpriseagentos.backendjava.domain.common.valueobjects.SubscriptionPlan;

public final class PlanLimits {
    public static final PlanLimit INDIVIDUAL_FREE = new PlanLimit(SubscriptionPlan.Free, 1, 500_000L);
    public static final PlanLimit INDIVIDUAL_PRO = new PlanLimit(SubscriptionPlan.Pro, 3, 10_000_000L);
    public static final PlanLimit ORG_FREE = new PlanLimit(SubscriptionPlan.Free, 1, 500_000L);
    public static final PlanLimit ORG_TEAM = new PlanLimit(SubscriptionPlan.Team, 10, 25_000_000L);

    private PlanLimits() {
    }

    public static PlanLimit forIndividualPlan(SubscriptionPlan plan) {
        return plan == SubscriptionPlan.Pro ? INDIVIDUAL_PRO : INDIVIDUAL_FREE;
    }

    public static PlanLimit forOrgPlan(SubscriptionPlan plan) {
        if (plan == SubscriptionPlan.Enterprise) {
            throw new IllegalArgumentException("Enterprise limits are stored on OrgSubscription.");
        }
        return plan == SubscriptionPlan.Team ? ORG_TEAM : ORG_FREE;
    }
}
