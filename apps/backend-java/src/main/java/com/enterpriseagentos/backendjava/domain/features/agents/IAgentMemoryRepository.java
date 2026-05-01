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

public interface IAgentMemoryRepository {
    CompletableFuture<AgentMemoryRecord> getAsync(UUID agentId, String key);
    CompletableFuture<List<AgentMemoryRecord>> listAsync(UUID agentId);
    CompletableFuture<Void> upsertAsync(UUID agentId, String key, String content);
    CompletableFuture<Boolean> deleteAsync(UUID agentId, String key);
}
