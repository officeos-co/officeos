package com.enterpriseagentos.backendjava.domain.features.agents.interfaces;

import java.util.List;
import java.util.concurrent.CompletableFuture;

import com.enterpriseagentos.backendjava.domain.features.agents.dtos.ProviderDto;

public interface IProviderService {
    CompletableFuture<List<ProviderDto>> listAsync();
    CompletableFuture<String> getApiKeyForDispatchAsync(String name);
}
