package com.enterpriseagentos.backendjava.domain.features.agents.interfaces;

import java.util.List;
import java.util.UUID;
import java.util.concurrent.CompletableFuture;

import com.enterpriseagentos.backendjava.domain.features.agents.models.AgentSessionModel;

public interface IAgentSessionRepository {
    CompletableFuture<AgentSessionModel> getActiveAsync(UUID agentId);
    CompletableFuture<AgentSessionModel> getByIdAsync(UUID sessionId);
    CompletableFuture<List<AgentSessionModel>> listByAgentAsync(UUID agentId, int limit);
    CompletableFuture<AgentSessionModel> createAsync(AgentSessionModel session);
    CompletableFuture<Void> saveChangesAsync();
    CompletableFuture<Integer> countByAgentAsync(UUID agentId);
}
