package com.enterpriseagentos.backendjava.domain.features.analytics.dtos;

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

import java.math.BigDecimal;
import java.time.Instant;
import java.time.YearMonth;
import java.util.*;
import java.util.concurrent.CompletableFuture;

public final class AuditEntry  {
    private final UUID id;
    private final UUID agentId;
    private final UUID userId;
    private final String skillName;
    private final String action;
    private final String paramsJson;
    private final String resultSummary;
    private final long durationMs;
    private final Instant timestamp;

    public AuditEntry(UUID id, UUID agentId, UUID userId, String skillName, String action, String paramsJson, String resultSummary, long durationMs, Instant timestamp) {
        this.id = id;
        this.agentId = agentId;
        this.userId = userId;
        this.skillName = skillName;
        this.action = action;
        this.paramsJson = paramsJson;
        this.resultSummary = resultSummary;
        this.durationMs = durationMs;
        this.timestamp = timestamp;
    }

    public UUID getId() {
        return id;
}

    public UUID id() {
        return id;
    }

    public UUID getAgentId() {
        return agentId;
}

    public UUID agentId() {
        return agentId;
    }

    public UUID getUserId() {
        return userId;
}

    public UUID userId() {
        return userId;
    }

    public String getSkillName() {
        return skillName;
}

    public String skillName() {
        return skillName;
    }

    public String getAction() {
        return action;
}

    public String action() {
        return action;
    }

    public String getParamsJson() {
        return paramsJson;
}

    public String paramsJson() {
        return paramsJson;
    }

    public String getResultSummary() {
        return resultSummary;
}

    public String resultSummary() {
        return resultSummary;
    }

    public long getDurationMs() {
        return durationMs;
}

    public long durationMs() {
        return durationMs;
    }

    public Instant getTimestamp() {
        return timestamp;
}

    public Instant timestamp() {
        return timestamp;
    }
}
