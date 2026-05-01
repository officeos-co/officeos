package com.enterpriseagentos.backendjava.domain.features.agents.interfaces;

import java.util.List;
import java.util.UUID;
import java.util.concurrent.CompletableFuture;

import com.enterpriseagentos.backendjava.domain.features.agents.models.AgentCronJobModel;

public interface IAgentCronJobRepository {
    CompletableFuture<List<AgentCronJobModel>> listAsync(UUID agentId);
    CompletableFuture<List<AgentCronJobModel>> listAllEnabledAsync();
    CompletableFuture<AgentCronJobModel> getAsync(UUID id);
    CompletableFuture<AgentCronJobModel> createAsync(UUID agentId, String name, String expression, String prompt);
    CompletableFuture<Void> updateAsync(AgentCronJobModel model);
    CompletableFuture<Void> setEnabledAsync(UUID id, boolean enabled);
    CompletableFuture<Boolean> deleteAsync(UUID id);
}
