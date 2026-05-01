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

public interface IAgentLogRepository {
    CompletableFuture<List<AgentLogRecord>> listAsync(UUID agentId, Instant before, int limit);
    CompletableFuture<List<AgentLogRecord>> listAfterAsync(UUID agentId, UUID afterLogId, int limit);
    CompletableFuture<AgentLogRecord> appendAsync(AgentLogRecord record);
    CompletableFuture<Void> appendPairAsync(AgentLogRecord toolCall, AgentLogRecord toolResult);
    CompletableFuture<AgentLogRecord> getByIdAsync(UUID id);
    CompletableFuture<Map<String, AgentLogRecord>> getResultsByCorrelationAsync(UUID agentId, Collection<String> correlationIds);
    CompletableFuture<Void> deleteByAgentIdsAsync(List<UUID> agentIds);
    CompletableFuture<List<AgentLogRecord>> listByAgentIdsAsync(List<UUID> agentIds, List<AgentLogType> types);
}
