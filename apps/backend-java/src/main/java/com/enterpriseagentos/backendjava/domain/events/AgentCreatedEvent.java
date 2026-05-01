package com.enterpriseagentos.backendjava.domain.events;

import java.util.UUID;

public record AgentCreatedEvent(UUID agentId, String provider, String model, UUID ownerId) implements DomainEvent {
}
