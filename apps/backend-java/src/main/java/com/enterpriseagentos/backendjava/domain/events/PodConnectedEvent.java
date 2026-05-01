package com.enterpriseagentos.backendjava.domain.events;

import java.util.UUID;

public final class PodConnectedEvent implements DomainEvent {
    private final UUID agentId;
    private final String correlationId;
    private final int durationMs;

    public PodConnectedEvent(UUID agentId, String correlationId, int durationMs) {
        this.agentId = agentId;
        this.correlationId = correlationId;
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

    public int getDurationMs() {
        return durationMs;
}

    public int durationMs() {
        return durationMs;
    }
}
