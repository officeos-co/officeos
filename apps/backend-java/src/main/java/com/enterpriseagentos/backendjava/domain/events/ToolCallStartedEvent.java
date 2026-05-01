package com.enterpriseagentos.backendjava.domain.events;

import java.util.UUID;

public final class ToolCallStartedEvent implements DomainEvent {
    private final UUID agentId;
    private final String correlationId;
    private final String toolName;
    private final String argsJson;

    public ToolCallStartedEvent(UUID agentId, String correlationId, String toolName, String argsJson) {
        this.agentId = agentId;
        this.correlationId = correlationId;
        this.toolName = toolName;
        this.argsJson = argsJson;
    }

    public UUID getAgentId() {
        return agentId;
}

    public UUID agentId() {
        return agentId;
    }

    public String getCorrelationId() {
        return correlationId;
}

    public String correlationId() {
        return correlationId;
    }

    public String getToolName() {
        return toolName;
}

    public String toolName() {
        return toolName;
    }

    public String getArgsJson() {
        return argsJson;
}

    public String argsJson() {
        return argsJson;
    }
}
