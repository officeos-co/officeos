package com.enterpriseagentos.backendjava.domain.features.management.registries;

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
