package com.enterpriseagentos.backendjava.domain.agents;

import java.util.List;
import java.util.Optional;
import java.util.UUID;

public interface AgentRepository {
    List<Agent> findAll();

    Optional<Agent> findById(UUID id);

    Agent save(Agent agent);
}
