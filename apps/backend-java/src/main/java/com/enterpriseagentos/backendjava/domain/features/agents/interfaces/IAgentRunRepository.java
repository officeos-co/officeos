package com.enterpriseagentos.backendjava.domain.features.agents.interfaces;

import java.util.List;
import java.util.UUID;
import java.util.concurrent.CompletableFuture;

import com.enterpriseagentos.backendjava.domain.features.agents.models.AgentRunModel;

public interface IAgentRunRepository {
    CompletableFuture<AgentRunModel> createAsync(AgentRunModel run);
    CompletableFuture<AgentRunModel> getAsync(UUID runId);
    CompletableFuture<List<AgentRunModel>> listForAgentAsync(UUID agentId, UUID parentRunId);
    CompletableFuture<Void> updateAsync(AgentRunModel run);
}
