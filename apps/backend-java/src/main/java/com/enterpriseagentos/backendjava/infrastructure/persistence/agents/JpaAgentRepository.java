package com.enterpriseagentos.backendjava.infrastructure.persistence.agents;

import java.util.UUID;

import org.springframework.data.jpa.repository.JpaRepository;

interface JpaAgentRepository extends JpaRepository<AgentEntity, UUID> {
}
