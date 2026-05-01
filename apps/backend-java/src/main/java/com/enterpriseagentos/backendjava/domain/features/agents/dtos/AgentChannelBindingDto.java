package com.enterpriseagentos.backendjava.domain.features.agents.dtos;

import java.time.Instant;
import java.util.UUID;

public final class AgentChannelBindingDto  {
    private final UUID id;
    private final UUID agentId;
    private final UUID channelConnectionId;
    private final String channelType;
    private final String channelDisplayName;
    private final boolean enabled;
    private final AgentChannelConfig config;
    private final Instant createdAt;

    public AgentChannelBindingDto(UUID id, UUID agentId, UUID channelConnectionId, String channelType, String channelDisplayName, boolean enabled, AgentChannelConfig config, Instant createdAt) {
        this.id = id;
        this.agentId = agentId;
        this.channelConnectionId = channelConnectionId;
        this.channelType = channelType;
        this.channelDisplayName = channelDisplayName;
        this.enabled = enabled;
        this.config = config;
        this.createdAt = createdAt;
    }

    public UUID getId() {
        return id;
}

    public UUID id() {
        return id;
    }

    public UUID getAgentId() {
        return agentId;
}

    public UUID agentId() {
        return agentId;
    }

    public UUID getChannelConnectionId() {
        return channelConnectionId;
}

    public UUID channelConnectionId() {
        return channelConnectionId;
    }

    public String getChannelType() {
        return channelType;
}

    public String channelType() {
        return channelType;
    }

    public String getChannelDisplayName() {
        return channelDisplayName;
}

    public String channelDisplayName() {
        return channelDisplayName;
    }

    public boolean getEnabled() {
        return enabled;
}

    public boolean enabled() {
        return enabled;
    }

    public AgentChannelConfig getConfig() {
        return config;
}

    public AgentChannelConfig config() {
        return config;
    }

    public Instant getCreatedAt() {
        return createdAt;
}

    public Instant createdAt() {
        return createdAt;
    }
}
