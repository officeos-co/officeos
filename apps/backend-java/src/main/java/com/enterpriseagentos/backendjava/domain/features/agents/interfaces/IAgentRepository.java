package com.enterpriseagentos.backendjava.domain.features.agents.interfaces;

import java.util.List;
import java.util.UUID;
import java.util.concurrent.CompletableFuture;

import com.enterpriseagentos.backendjava.domain.common.valueobjects.AgentStatus;
import com.enterpriseagentos.backendjava.domain.features.agents.models.AgentModel;

public interface IAgentRepository {
    CompletableFuture<List<AgentModel>> listAsync();
    CompletableFuture<AgentModel> getAsync(UUID id);
    CompletableFuture<Void> addAsync(AgentModel model);
    CompletableFuture<Void> updateAsync(AgentModel model);
    CompletableFuture<Boolean> softDeleteAsync(UUID id);
    CompletableFuture<Void> updateStatusAsync(UUID id, AgentStatus status);
    CompletableFuture<List<AgentModel>> listByOwnerAsync(UUID ownerId, boolean includeDeleted);
    CompletableFuture<Void> hardDeleteByOwnerAsync(UUID ownerId);
}
