package com.enterpriseagentos.backendjava.domain.events;

import java.util.UUID;

public final class AgentDeletedEvent implements DomainEvent {
    private final UUID agentId;
    private final String podName;
    private final boolean hasPod;
    private final UUID ownerId;

    public AgentDeletedEvent(UUID agentId, String podName, boolean hasPod, UUID ownerId) {
        this.agentId = agentId;
        this.podName = podName;
        this.hasPod = hasPod;
        this.ownerId = ownerId;
    }

    public UUID getAgentId() {
        return agentId;
}

    public UUID agentId() {
        return agentId;
    }

    public String getPodName() {
        return podName;
}

    public String podName() {
        return podName;
    }

    public boolean getHasPod() {
        return hasPod;
}

    public boolean hasPod() {
        return hasPod;
    }

    public UUID getOwnerId() {
        return ownerId;
}

    public UUID ownerId() {
        return ownerId;
    }
}
