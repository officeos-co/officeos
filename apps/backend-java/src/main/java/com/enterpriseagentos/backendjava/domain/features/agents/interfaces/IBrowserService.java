package com.enterpriseagentos.backendjava.domain.features.agents.interfaces;

import java.util.UUID;
import java.util.concurrent.CompletableFuture;

import com.enterpriseagentos.backendjava.domain.features.agents.dtos.BrowserSessionState;

public interface IBrowserService {
    CompletableFuture<BrowserSessionState> getOrCreateAsync(UUID agentId);
    CompletableFuture<BrowserSessionState> getStateAsync(UUID agentId);
    CompletableFuture<BrowserSessionState> restartAsync(UUID agentId);
    CompletableFuture<Void> stopAsync(UUID agentId);
    CompletableFuture<String> getViewUrlAsync(UUID agentId);
}
