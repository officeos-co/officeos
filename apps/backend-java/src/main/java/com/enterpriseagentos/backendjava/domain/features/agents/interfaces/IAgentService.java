package com.enterpriseagentos.backendjava.domain.features.agents.interfaces;

import java.util.List;
import java.util.UUID;
import java.util.concurrent.CompletableFuture;

import com.enterpriseagentos.backendjava.domain.features.agents.dtos.AgentDto;
import com.enterpriseagentos.backendjava.domain.features.agents.dtos.AgentInitRequest;
import com.enterpriseagentos.backendjava.domain.features.agents.dtos.CreateAgentRequest;
import com.enterpriseagentos.backendjava.domain.features.agents.dtos.PatchAgentRequest;

public interface IAgentService {
    CompletableFuture<List<AgentDto>> listAsync();
    CompletableFuture<AgentDto> getAsync(UUID id);
    CompletableFuture<AgentDto> createAsync(CreateAgentRequest request, UUID ownerId);
    CompletableFuture<AgentDto> patchAsync(UUID id, PatchAgentRequest request);
    CompletableFuture<Boolean> deleteAsync(UUID id);
    CompletableFuture<Void> initializeAgentAsync(UUID agentId, UUID userId, AgentInitRequest init);
}
