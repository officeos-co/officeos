package com.enterpriseagentos.backendjava.domain.features.agents;

import java.math.BigDecimal;
import java.time.Instant;
import java.time.YearMonth;
import java.util.*;
import java.util.concurrent.CompletableFuture;
import com.enterpriseagentos.backendjava.domain.common.primitives.*;
import com.enterpriseagentos.backendjava.domain.common.services.*;
import com.enterpriseagentos.backendjava.domain.common.valueobjects.*;
import com.enterpriseagentos.backendjava.domain.events.*;
import com.enterpriseagentos.backendjava.domain.features.agents.*;
import com.enterpriseagentos.backendjava.domain.features.analytics.*;
import com.enterpriseagentos.backendjava.domain.features.management.*;
import com.enterpriseagentos.backendjava.domain.features.mcp.*;

public interface IBrowserRuntimeClient {
    CompletableFuture<Boolean> isAvailableAsync();
    CompletableFuture<BrowserSessionState> getSessionAsync(UUID agentId, String runtimeSessionId);
    CompletableFuture<BrowserSessionState> createSessionAsync(UUID agentId, String name, String authProfile);
    CompletableFuture<Void> closeSessionAsync(String runtimeSessionId);
    CompletableFuture<List<BrowserToolDescriptor>> listToolsAsync();
    CompletableFuture<BrowserToolCallResult> callToolAsync(String name, Map<String, Object> arguments);
}
