package com.enterpriseagentos.backendjava.domain.features.agents.interfaces;

import java.util.concurrent.CompletableFuture;

public interface IAgentSandbox {
    CompletableFuture<Boolean> terminateAsync(String sandboxId);
}
