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

public interface IAgentRepository {
    CompletableFuture<List<AgentRecord>> listAsync();
    CompletableFuture<AgentRecord> getAsync(UUID id);
    CompletableFuture<Void> addAsync(AgentRecord record);
    CompletableFuture<Void> updateAsync(AgentRecord record);
    CompletableFuture<Boolean> softDeleteAsync(UUID id);
    CompletableFuture<Void> updateStatusAsync(UUID id, AgentStatus status);
    CompletableFuture<List<AgentRecord>> listByOwnerAsync(UUID ownerId, boolean includeDeleted);
    CompletableFuture<Void> hardDeleteByOwnerAsync(UUID ownerId);
}
