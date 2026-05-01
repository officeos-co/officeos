package com.enterpriseagentos.backendjava.domain.features.agents.interfaces;

import java.util.List;
import java.util.UUID;
import java.util.concurrent.CompletableFuture;

import com.enterpriseagentos.backendjava.domain.features.agents.models.AgentPersonalityModel;

public interface IAgentPersonalityRepository {
    CompletableFuture<List<AgentPersonalityModel>> listAsync(UUID agentId);
    CompletableFuture<Void> upsertAsync(UUID agentId, String fileName, String content);
}
