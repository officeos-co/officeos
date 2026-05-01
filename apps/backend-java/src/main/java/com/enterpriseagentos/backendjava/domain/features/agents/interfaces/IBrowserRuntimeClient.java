package com.enterpriseagentos.backendjava.domain.features.agents.interfaces;

import java.util.List;
import java.util.Map;
import java.util.UUID;
import java.util.concurrent.CompletableFuture;

import com.enterpriseagentos.backendjava.domain.features.agents.dtos.BrowserSessionState;
import com.enterpriseagentos.backendjava.domain.features.agents.dtos.BrowserToolCallResult;
import com.enterpriseagentos.backendjava.domain.features.agents.dtos.BrowserToolDescriptor;

public interface IBrowserRuntimeClient {
    CompletableFuture<Boolean> isAvailableAsync();
    CompletableFuture<BrowserSessionState> getSessionAsync(UUID agentId, String runtimeSessionId);
    CompletableFuture<BrowserSessionState> createSessionAsync(UUID agentId, String name, String authProfile);
    CompletableFuture<Void> closeSessionAsync(String runtimeSessionId);
    CompletableFuture<List<BrowserToolDescriptor>> listToolsAsync();
    CompletableFuture<BrowserToolCallResult> callToolAsync(String name, Map<String, Object> arguments);
}
