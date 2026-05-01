package com.enterpriseagentos.backendjava.domain.features.agents.interfaces;

import java.util.List;
import java.util.UUID;
import java.util.concurrent.CompletableFuture;

import com.enterpriseagentos.backendjava.domain.features.agents.dtos.AgentTemplateDto;

public interface IAgentTemplateService {
    CompletableFuture<List<AgentTemplateDto>> listAsync();
    CompletableFuture<AgentTemplateDto> getAsync(UUID id);
}
