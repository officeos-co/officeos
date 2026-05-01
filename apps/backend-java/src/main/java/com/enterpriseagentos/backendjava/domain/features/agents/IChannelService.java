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

public interface IChannelService {
    CompletableFuture<List<UUID>> routeInboundAsync(UUID connectionId, String senderIdentifier, String messageText, boolean isGroupMessage, String messageId, String channelId);
    CompletableFuture<List<UUID>> routeInboundByChannelTypeAsync(String channelType, String senderIdentifier, String messageText, boolean isGroupMessage, String messageId, String channelId);
    CompletableFuture<Void> broadcastAsync(UUID agentId, String text);
    CompletableFuture<Void> sendTestMessageAsync(UUID connectionId);
    CompletableFuture<ChannelConnectionRecord> createConnectionAsync(String channelType, String displayName, String configJson, UUID createdById);
    CompletableFuture<ChannelConnectionRecord> updateConnectionAsync(UUID id, String displayName, boolean enabled);
    CompletableFuture<Boolean> deleteConnectionAsync(UUID id);
    CompletableFuture<Void> saveChannelCredsAsync(UUID connectionId, String credsJson);
    CompletableFuture<AgentChannelBindingRecord> bindAgentAsync(UUID agentId, UUID channelConnectionId, String configJson);
    CompletableFuture<Boolean> unbindAgentAsync(UUID agentId, UUID channelConnectionId);
    CompletableFuture<AgentChannelBindingRecord> updateBindingConfigAsync(UUID agentId, UUID channelConnectionId, String configJson);
}
