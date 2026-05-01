package com.enterpriseagentos.backendjava.domain.events;

import java.util.UUID;

public final class AgentCreatedEvent implements DomainEvent {
    private final UUID agentId;
    private final String provider;
    private final String model;
    private final UUID ownerId;

    public AgentCreatedEvent(UUID agentId, String provider, String model, UUID ownerId) {
        this.agentId = agentId;
        this.provider = provider;
        this.model = model;
        this.ownerId = ownerId;
    }

    public UUID getAgentId() {
        return agentId;
}

    public UUID agentId() {
        return agentId;
    }

    public String getProvider() {
        return provider;
}

    public String provider() {
        return provider;
    }

    public String getModel() {
        return model;
}

    public String model() {
        return model;
    }

    public UUID getOwnerId() {
        return ownerId;
}

    public UUID ownerId() {
        return ownerId;
    }
}
