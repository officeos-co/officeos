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

public interface IChannelRepository {
    CompletableFuture<List<ChannelConnectionRecord>> listConnectionsAsync();
    CompletableFuture<ChannelConnectionRecord> getConnectionAsync(UUID id);
    CompletableFuture<ChannelConnectionRecord> createConnectionAsync(ChannelConnectionRecord record);
    CompletableFuture<ChannelConnectionRecord> updateConnectionAsync(UUID id, java.util.function.Consumer<ChannelConnectionRecord> apply);
    CompletableFuture<Boolean> deleteConnectionAsync(UUID id);
    CompletableFuture<List<AgentChannelBindingRecord>> listBindingsAsync(UUID agentId);
    CompletableFuture<AgentChannelBindingRecord> getBindingAsync(UUID bindingId);
    CompletableFuture<AgentChannelBindingRecord> createBindingAsync(AgentChannelBindingRecord record);
    CompletableFuture<AgentChannelBindingRecord> updateBindingAsync(UUID bindingId, java.util.function.Consumer<AgentChannelBindingRecord> apply);
    CompletableFuture<Boolean> deleteBindingAsync(UUID bindingId);
    CompletableFuture<List<AgentChannelBindingRecord>> findBindingsByConnectionAsync(UUID connectionId);
    CompletableFuture<List<ChannelConnectionRecord>> findConnectionsByChannelTypeAsync(String channelType);
}
