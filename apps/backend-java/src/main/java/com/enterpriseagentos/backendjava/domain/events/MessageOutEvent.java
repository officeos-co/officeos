package com.enterpriseagentos.backendjava.domain.events;

import java.util.UUID;

public final class MessageOutEvent implements DomainEvent {
    private final UUID agentId;
    private final String correlationId;
    private final String content;

    public MessageOutEvent(UUID agentId, String correlationId, String content) {
        this.agentId = agentId;
        this.correlationId = correlationId;
        this.content = content;
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

    public String getContent() {
        return content;
}

    public String content() {
        return content;
    }
}
