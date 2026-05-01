package com.enterpriseagentos.backendjava.domain.events;

import java.util.UUID;

public final class MessageReceivedEvent implements DomainEvent {
    private final UUID agentId;
    private final String content;
    private final String correlationId;

    public MessageReceivedEvent(UUID agentId, String content, String correlationId) {
        this.agentId = agentId;
        this.content = content;
        this.correlationId = correlationId;
    }

    public UUID getAgentId() {
        return agentId;
}

    public UUID agentId() {
        return agentId;
    }

    public String getContent() {
        return content;
}

    public String content() {
        return content;
    }

    public String getCorrelationId() {
        return correlationId;
}

    public String correlationId() {
        return correlationId;
    }
}
