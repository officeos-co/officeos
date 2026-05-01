package com.enterpriseagentos.backendjava.domain.events;

import java.util.UUID;

public final class ConversationCompactedEvent implements DomainEvent {
    private final UUID agentId;
    private final String correlationId;
    private final UUID lastCompactedLogId;
    private final int preCompactTokens;
    private final int postCompactTokens;

    public ConversationCompactedEvent(UUID agentId, String correlationId, UUID lastCompactedLogId, int preCompactTokens, int postCompactTokens) {
        this.agentId = agentId;
        this.correlationId = correlationId;
        this.lastCompactedLogId = lastCompactedLogId;
        this.preCompactTokens = preCompactTokens;
        this.postCompactTokens = postCompactTokens;
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

    public UUID getLastCompactedLogId() {
        return lastCompactedLogId;
}

    public UUID lastCompactedLogId() {
        return lastCompactedLogId;
    }

    public int getPreCompactTokens() {
        return preCompactTokens;
}

    public int preCompactTokens() {
        return preCompactTokens;
    }

    public int getPostCompactTokens() {
        return postCompactTokens;
}

    public int postCompactTokens() {
        return postCompactTokens;
    }
}
