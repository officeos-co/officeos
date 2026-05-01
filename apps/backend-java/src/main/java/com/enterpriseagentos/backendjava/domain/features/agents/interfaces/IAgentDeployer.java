package com.enterpriseagentos.backendjava.domain.features.agents.interfaces;

import java.util.concurrent.CompletableFuture;

public interface IAgentDeployer {
    CompletableFuture<Boolean> removeAsync(String podName);
    CompletableFuture<String> getStatusAsync(String podName);
    CompletableFuture<String> getLogsAsync(String podName, int tailLines);
}
