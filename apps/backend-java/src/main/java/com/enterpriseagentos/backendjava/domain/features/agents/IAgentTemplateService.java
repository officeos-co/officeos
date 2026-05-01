package com.enterpriseagentos.backendjava.domain.features.agents;

import java.util.List;
import java.util.UUID;
import java.util.concurrent.CompletableFuture;

public interface IAgentTemplateService {
    CompletableFuture<List<AgentTemplateDto>> listAsync();
    CompletableFuture<AgentTemplateDto> getAsync(UUID id);
}
