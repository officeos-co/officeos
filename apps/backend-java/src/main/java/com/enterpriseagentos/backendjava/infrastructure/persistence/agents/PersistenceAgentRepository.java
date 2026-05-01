package com.enterpriseagentos.backendjava.infrastructure.persistence.agents;

import com.enterpriseagentos.backendjava.domain.agents.Agent;
import com.enterpriseagentos.backendjava.domain.agents.AgentRepository;
import java.util.List;
import java.util.Optional;
import java.util.UUID;
import org.springframework.stereotype.Repository;

@Repository
public class PersistenceAgentRepository implements AgentRepository {
    private final JpaAgentRepository jpaRepository;

    public PersistenceAgentRepository(JpaAgentRepository jpaRepository) {
        this.jpaRepository = jpaRepository;
    }

    @Override
    public List<Agent> findAll() {
        return jpaRepository.findAll().stream()
            .map(AgentMapper::toDomain)
            .toList();
    }

    @Override
    public Optional<Agent> findById(UUID id) {
        return jpaRepository.findById(id).map(AgentMapper::toDomain);
    }

    @Override
    public Agent save(Agent agent) {
        return AgentMapper.toDomain(jpaRepository.save(AgentMapper.toEntity(agent)));
    }
}
