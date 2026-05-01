package com.enterpriseagentos.backendjava.infrastructure.persistence.agents;

import com.enterpriseagentos.backendjava.domain.agents.Agent;

final class AgentMapper {
    private AgentMapper() {
    }

    static Agent toDomain(AgentEntity entity) {
        return new Agent(entity.id(), entity.name(), entity.status(), entity.createdAt());
    }

    static AgentEntity toEntity(Agent agent) {
        return new AgentEntity(agent.id(), agent.name(), agent.status(), agent.createdAt());
    }
}
