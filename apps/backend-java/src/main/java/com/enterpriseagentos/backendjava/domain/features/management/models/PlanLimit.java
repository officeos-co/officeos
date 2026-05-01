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

public final class PlanLimit  {
    private final SubscriptionPlan plan;
    private final int concurrentAgents;
    private final long creditsPerMonth;

    public PlanLimit(SubscriptionPlan plan, int concurrentAgents, long creditsPerMonth) {
        this.plan = plan;
        this.concurrentAgents = concurrentAgents;
        this.creditsPerMonth = creditsPerMonth;
    }

    public SubscriptionPlan getPlan() {
        return plan;
}

    public SubscriptionPlan plan() {
        return plan;
    }

    public int getConcurrentAgents() {
        return concurrentAgents;
}

    public int concurrentAgents() {
        return concurrentAgents;
    }

    public long getCreditsPerMonth() {
        return creditsPerMonth;
}

    public long creditsPerMonth() {
        return creditsPerMonth;
    }

    public String description() {
        String agentWord = concurrentAgents == 1 ? "agent" : "agents";
        String credits = creditsPerMonth >= 1_000_000
            ? (creditsPerMonth / 1_000_000) + "M"
            : (creditsPerMonth / 1_000) + "k";
        return concurrentAgents + " concurrent " + agentWord + ", " + credits + " credits/month";
    }
}
