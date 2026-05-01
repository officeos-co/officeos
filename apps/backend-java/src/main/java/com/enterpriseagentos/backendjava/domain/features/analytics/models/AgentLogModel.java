package com.enterpriseagentos.backendjava.domain.features.analytics.models;

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

public class AgentLogModel {
    private UUID id;
    private UUID agentId;
    private AgentModel agent;
    private Instant time;
    private AgentLogType type;
    private String tool;
    private String integration;
    private String channel;
    private String content;
    private TokenUsage usage;
    private String correlationId;
    private UUID runId;
    private UUID parentRunId;

    private AgentLogModel() {
    }

    public static AgentLogModel error(UUID agentId, AgentLogType type, String content, Instant time) {
        AgentLogModel model = new AgentLogModel();
        model.agentId = agentId;
        model.type = type;
        model.content = content;
        model.time = time;
        return model;
    }

    public UUID getId() {
        return id;
    }

    public UUID getAgentId() {
        return agentId;
    }

    public AgentModel getAgent() {
        return agent;
    }

    public Instant getTime() {
        return time;
    }

    public AgentLogType getType() {
        return type;
    }

    public String getTool() {
        return tool;
    }

    public String getIntegration() {
        return integration;
    }

    public String getChannel() {
        return channel;
    }

    public String getContent() {
        return content;
    }

    public TokenUsage getUsage() {
        return usage;
    }

    public String getCorrelationId() {
        return correlationId;
    }

    public UUID getRunId() {
        return runId;
    }

    public UUID getParentRunId() {
        return parentRunId;
    }
}
