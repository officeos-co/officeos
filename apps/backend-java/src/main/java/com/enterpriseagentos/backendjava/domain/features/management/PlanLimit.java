package com.enterpriseagentos.backendjava.domain.features.management;

import com.enterpriseagentos.backendjava.domain.common.valueobjects.SubscriptionPlan;

public record PlanLimit(SubscriptionPlan plan, int concurrentAgents, long creditsPerMonth) {
    public String description() {
        String agentWord = concurrentAgents == 1 ? "agent" : "agents";
        String credits = creditsPerMonth >= 1_000_000
            ? (creditsPerMonth / 1_000_000) + "M"
            : (creditsPerMonth / 1_000) + "k";
        return concurrentAgents + " concurrent " + agentWord + ", " + credits + " credits/month";
    }
}
