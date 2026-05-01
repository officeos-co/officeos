package com.enterpriseagentos.backendjava.infrastructure.persistence.toolinvocations;

import java.util.List;
import java.util.UUID;
import org.springframework.data.jpa.repository.JpaRepository;

interface JpaToolInvocationRepository extends JpaRepository<ToolInvocationEntity, UUID> {
    List<ToolInvocationEntity> findByAgentIdOrderByCreatedAtDesc(UUID agentId);
}
