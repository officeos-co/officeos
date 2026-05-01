package com.enterpriseagentos.backendjava.domain.events;

import java.util.UUID;

public final class AgentUpdatedEvent implements DomainEvent {
    private final UUID agentId;

    public AgentUpdatedEvent(UUID agentId) {
        this.agentId = agentId;
    }

    public UUID getAgentId() {
        return agentId;
}

    public UUID agentId() {
        return agentId;
    }
}
