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

public final class AppendAgentLogInput  {
    private final UUID agentId;
    private final AgentLogType type;
    private final String content;
    private final String tool;
    private final String integration;
    private final String channel;
    private final String correlationId;

    public AppendAgentLogInput(UUID agentId, AgentLogType type, String content, String tool, String integration, String channel, String correlationId) {
        this.agentId = agentId;
        this.type = type;
        this.content = content;
        this.tool = tool;
        this.integration = integration;
        this.channel = channel;
        this.correlationId = correlationId;
    }

    public UUID getAgentId() {
        return agentId;
}

    public UUID agentId() {
        return agentId;
    }

    public AgentLogType getType() {
        return type;
}

    public AgentLogType type() {
        return type;
    }

    public String getContent() {
        return content;
}

    public String content() {
        return content;
    }

    public String getTool() {
        return tool;
}

    public String tool() {
        return tool;
    }

    public String getIntegration() {
        return integration;
}

    public String integration() {
        return integration;
    }

    public String getChannel() {
        return channel;
}

    public String channel() {
        return channel;
    }

    public String getCorrelationId() {
        return correlationId;
}

    public String correlationId() {
        return correlationId;
    }
}
