package com.enterpriseagentos.backendjava.domain.features.agents.interfaces;

import java.util.List;
import java.util.UUID;
import java.util.concurrent.CompletableFuture;

import com.enterpriseagentos.backendjava.domain.features.agents.models.AgentChannelBindingModel;
import com.enterpriseagentos.backendjava.domain.features.agents.models.ChannelConnectionModel;

public interface IChannelService {
    CompletableFuture<List<UUID>> routeInboundAsync(UUID connectionId, String senderIdentifier, String messageText, boolean isGroupMessage, String messageId, String channelId);
    CompletableFuture<List<UUID>> routeInboundByChannelTypeAsync(String channelType, String senderIdentifier, String messageText, boolean isGroupMessage, String messageId, String channelId);
    CompletableFuture<Void> broadcastAsync(UUID agentId, String text);
    CompletableFuture<Void> sendTestMessageAsync(UUID connectionId);
    CompletableFuture<ChannelConnectionModel> createConnectionAsync(String channelType, String displayName, String configJson, UUID createdById);
    CompletableFuture<ChannelConnectionModel> updateConnectionAsync(UUID id, String displayName, boolean enabled);
    CompletableFuture<Boolean> deleteConnectionAsync(UUID id);
    CompletableFuture<Void> saveChannelCredsAsync(UUID connectionId, String credsJson);
    CompletableFuture<AgentChannelBindingModel> bindAgentAsync(UUID agentId, UUID channelConnectionId, String configJson);
    CompletableFuture<Boolean> unbindAgentAsync(UUID agentId, UUID channelConnectionId);
    CompletableFuture<AgentChannelBindingModel> updateBindingConfigAsync(UUID agentId, UUID channelConnectionId, String configJson);
}
