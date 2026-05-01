package com.enterpriseagentos.backendjava.infrastructure.persistence.toolinvocations;

import com.enterpriseagentos.backendjava.domain.agents.ToolInvocation;
import com.enterpriseagentos.backendjava.domain.agents.ToolInvocationRepository;
import java.util.List;
import java.util.UUID;
import org.springframework.stereotype.Repository;

@Repository
public class PersistenceToolInvocationRepository implements ToolInvocationRepository {
    private final JpaToolInvocationRepository jpaRepository;

    public PersistenceToolInvocationRepository(JpaToolInvocationRepository jpaRepository) {
        this.jpaRepository = jpaRepository;
    }

    @Override
    public List<ToolInvocation> findByAgentId(UUID agentId) {
        return jpaRepository.findByAgentIdOrderByCreatedAtDesc(agentId).stream()
            .map(ToolInvocationMapper::toDomain)
            .toList();
    }

    @Override
    public ToolInvocation save(ToolInvocation invocation) {
        return ToolInvocationMapper.toDomain(jpaRepository.save(ToolInvocationMapper.toEntity(invocation)));
    }
}
