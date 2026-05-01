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

public final class AgentLogDto  {
    private final UUID id;
    private final UUID agentId;
    private final String agentName;
    private final Instant time;
    private final AgentLogType type;
    private final String tool;
    private final String integration;
    private final String channel;
    private final String content;
    private final int durationMs;
    private final int inputTokens;
    private final int outputTokens;
    private final String correlationId;

    public AgentLogDto(UUID id, UUID agentId, String agentName, Instant time, AgentLogType type, String tool, String integration, String channel, String content, int durationMs, int inputTokens, int outputTokens, String correlationId) {
        this.id = id;
        this.agentId = agentId;
        this.agentName = agentName;
        this.time = time;
        this.type = type;
        this.tool = tool;
        this.integration = integration;
        this.channel = channel;
        this.content = content;
        this.durationMs = durationMs;
        this.inputTokens = inputTokens;
        this.outputTokens = outputTokens;
        this.correlationId = correlationId;
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

    public String getAgentName() {
        return agentName;
}

    public String agentName() {
        return agentName;
    }

    public Instant getTime() {
        return time;
}

    public Instant time() {
        return time;
    }

    public AgentLogType getType() {
        return type;
}

    public AgentLogType type() {
        return type;
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

    public String getContent() {
        return content;
}

    public String content() {
        return content;
    }

    public int getDurationMs() {
        return durationMs;
}

    public int durationMs() {
        return durationMs;
    }

    public int getInputTokens() {
        return inputTokens;
}

    public int inputTokens() {
        return inputTokens;
    }

    public int getOutputTokens() {
        return outputTokens;
}

    public int outputTokens() {
        return outputTokens;
    }

    public String getCorrelationId() {
        return correlationId;
}

    public String correlationId() {
        return correlationId;
    }
}
