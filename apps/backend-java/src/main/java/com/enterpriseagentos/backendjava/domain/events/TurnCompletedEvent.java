package com.enterpriseagentos.backendjava.domain.events;

import java.util.UUID;

public final class TurnCompletedEvent implements DomainEvent {
    private final UUID agentId;
    private final String correlationId;
    private final int durationMs;
    private final int iterations;
    private final int toolCallCount;

    public TurnCompletedEvent(UUID agentId, String correlationId, int durationMs, int iterations, int toolCallCount) {
        this.agentId = agentId;
        this.correlationId = correlationId;
        this.durationMs = durationMs;
        this.iterations = iterations;
        this.toolCallCount = toolCallCount;
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

    public int getDurationMs() {
        return durationMs;
}

    public int durationMs() {
        return durationMs;
    }

    public int getIterations() {
        return iterations;
}

    public int iterations() {
        return iterations;
    }

    public int getToolCallCount() {
        return toolCallCount;
}

    public int toolCallCount() {
        return toolCallCount;
    }
}
