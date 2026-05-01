package com.enterpriseagentos.backendjava.domain.features.agents.dtos;

import java.util.UUID;

public final class CreateAgentChannelBindingRequest  {
    private final UUID channelConnectionId;
    private final AgentChannelConfig config;

    public CreateAgentChannelBindingRequest(UUID channelConnectionId, AgentChannelConfig config) {
        this.channelConnectionId = channelConnectionId;
        this.config = config;
    }

    public UUID getChannelConnectionId() {
        return channelConnectionId;
}

    public UUID channelConnectionId() {
        return channelConnectionId;
    }

    public AgentChannelConfig getConfig() {
        return config;
}

    public AgentChannelConfig config() {
        return config;
    }
}
