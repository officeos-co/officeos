package com.enterpriseagentos.backendjava.domain.features.agents.interfaces;

import java.util.List;
import java.util.UUID;
import java.util.concurrent.CompletableFuture;

import com.enterpriseagentos.backendjava.domain.features.agents.models.AgentChannelBindingModel;
import com.enterpriseagentos.backendjava.domain.features.agents.models.ChannelConnectionModel;

public interface IChannelRepository {
    CompletableFuture<List<ChannelConnectionModel>> listConnectionsAsync();
    CompletableFuture<ChannelConnectionModel> getConnectionAsync(UUID id);
    CompletableFuture<ChannelConnectionModel> createConnectionAsync(ChannelConnectionModel model);
    CompletableFuture<ChannelConnectionModel> updateConnectionAsync(UUID id, java.util.function.Consumer<ChannelConnectionModel> apply);
    CompletableFuture<Boolean> deleteConnectionAsync(UUID id);
    CompletableFuture<List<AgentChannelBindingModel>> listBindingsAsync(UUID agentId);
    CompletableFuture<AgentChannelBindingModel> getBindingAsync(UUID bindingId);
    CompletableFuture<AgentChannelBindingModel> createBindingAsync(AgentChannelBindingModel model);
    CompletableFuture<AgentChannelBindingModel> updateBindingAsync(UUID bindingId, java.util.function.Consumer<AgentChannelBindingModel> apply);
    CompletableFuture<Boolean> deleteBindingAsync(UUID bindingId);
    CompletableFuture<List<AgentChannelBindingModel>> findBindingsByConnectionAsync(UUID connectionId);
    CompletableFuture<List<ChannelConnectionModel>> findConnectionsByChannelTypeAsync(String channelType);
}
