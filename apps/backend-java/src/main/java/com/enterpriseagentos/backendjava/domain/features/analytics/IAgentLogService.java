package com.enterpriseagentos.backendjava.domain.features.analytics;

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

public interface IAgentLogService {
    CompletableFuture<List<AgentLogRecord>> listForAgentAsync(UUID agentId, Instant before, int limit);
    CompletableFuture<GlobalLogsPage> listGlobalAsync(GlobalLogFiltersInput filters);
    CompletableFuture<AgentLogRecord> appendAsync(AgentLogRecord record);
    CompletableFuture<AgentLogRecord> sendMessageAsync(UUID agentId, String content, UUID userId);
    CompletableFuture<Map<String, AgentLogRecord>> getResultsByCorrelationAsync(UUID agentId, Collection<String> correlationIds);
}
