package com.enterpriseagentos.backendjava.domain.features.agents.interfaces;

import java.util.concurrent.CompletableFuture;

public interface IChannelGateway {
    CompletableFuture<Void> reloadAsync();
}
