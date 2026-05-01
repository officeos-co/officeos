package com.enterpriseagentos.backendjava.domain.features.agents.interfaces;

import java.util.List;
import java.util.UUID;
import java.util.concurrent.CompletableFuture;

import com.enterpriseagentos.backendjava.domain.features.agents.models.AgentMemoryModel;

public interface IAgentMemoryRepository {
    CompletableFuture<AgentMemoryModel> getAsync(UUID agentId, String key);
    CompletableFuture<List<AgentMemoryModel>> listAsync(UUID agentId);
    CompletableFuture<Void> upsertAsync(UUID agentId, String key, String content);
    CompletableFuture<Boolean> deleteAsync(UUID agentId, String key);
}
