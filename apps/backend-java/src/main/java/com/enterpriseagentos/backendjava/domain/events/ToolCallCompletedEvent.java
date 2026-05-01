package com.enterpriseagentos.backendjava.domain.events;

import java.util.UUID;

public final class ToolCallCompletedEvent implements DomainEvent {
    private final UUID agentId;
    private final String correlationId;
    private final String toolName;
    private final boolean success;
    private final String output;
    private final int durationMs;

    public ToolCallCompletedEvent(UUID agentId, String correlationId, String toolName, boolean success, String output, int durationMs) {
        this.agentId = agentId;
        this.correlationId = correlationId;
        this.toolName = toolName;
        this.success = success;
        this.output = output;
        this.durationMs = durationMs;
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

    public boolean getSuccess() {
        return success;
}

    public boolean success() {
        return success;
    }

    public String getOutput() {
        return output;
}

    public String output() {
        return output;
    }

    public int getDurationMs() {
        return durationMs;
}

    public int durationMs() {
        return durationMs;
    }
}
