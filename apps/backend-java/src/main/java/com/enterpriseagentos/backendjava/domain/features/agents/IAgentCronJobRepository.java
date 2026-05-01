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

public interface IAgentCronJobRepository {
    CompletableFuture<List<AgentCronJobRecord>> listAsync(UUID agentId);
    CompletableFuture<List<AgentCronJobRecord>> listAllEnabledAsync();
    CompletableFuture<AgentCronJobRecord> getAsync(UUID id);
    CompletableFuture<AgentCronJobRecord> createAsync(UUID agentId, String name, String expression, String prompt);
    CompletableFuture<Void> updateAsync(AgentCronJobRecord record);
    CompletableFuture<Void> setEnabledAsync(UUID id, boolean enabled);
    CompletableFuture<Boolean> deleteAsync(UUID id);
}
