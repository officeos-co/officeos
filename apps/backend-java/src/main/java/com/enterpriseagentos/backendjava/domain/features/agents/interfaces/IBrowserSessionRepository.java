package com.enterpriseagentos.backendjava.domain.features.agents.interfaces;

import java.util.UUID;
import java.util.concurrent.CompletableFuture;

import com.enterpriseagentos.backendjava.domain.features.agents.models.BrowserSessionModel;

public interface IBrowserSessionRepository {
    CompletableFuture<BrowserSessionModel> getByAgentAsync(UUID agentId);
    CompletableFuture<BrowserSessionModel> upsertAsync(UUID agentId, String runtimeSessionId, String cookiesJson);
    CompletableFuture<Void> deleteByAgentAsync(UUID agentId);
}
