package com.enterpriseagentos.backendjava.domain.events;

import java.util.UUID;

public final class TurnStartedEvent implements DomainEvent {
    private final UUID agentId;
    private final String correlationId;
    private final String userMessage;

    public TurnStartedEvent(UUID agentId, String correlationId, String userMessage) {
        this.agentId = agentId;
        this.correlationId = correlationId;
        this.userMessage = userMessage;
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

    public String getUserMessage() {
        return userMessage;
}

    public String userMessage() {
        return userMessage;
    }
}
