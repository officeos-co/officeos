package com.enterpriseagentos.backendjava.domain.events;

import java.util.UUID;

public final class AgentErrorOccurredEvent implements DomainEvent {
    private final UUID agentId;
    private final String correlationId;
    private final String message;

    public AgentErrorOccurredEvent(UUID agentId, String correlationId, String message) {
        this.agentId = agentId;
        this.correlationId = correlationId;
        this.message = message;
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

    public String getMessage() {
        return message;
}

    public String message() {
        return message;
    }
}
