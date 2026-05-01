package com.enterpriseagentos.backendjava.domain.features.agents.interfaces;

import java.util.UUID;
import java.util.concurrent.CompletableFuture;

import com.enterpriseagentos.backendjava.domain.features.agents.models.AgentSessionContextModel;

public interface IAgentSessionContextRepository {
    CompletableFuture<AgentSessionContextModel> getAsync(UUID agentId);
    CompletableFuture<Void> upsertAsync(AgentSessionContextModel context);
}
