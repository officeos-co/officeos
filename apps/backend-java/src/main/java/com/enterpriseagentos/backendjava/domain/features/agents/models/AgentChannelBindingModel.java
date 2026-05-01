package com.enterpriseagentos.backendjava.domain.features.agents.models;

import java.time.Instant;
import java.util.UUID;

public class AgentChannelBindingModel {
    private UUID id;
    private UUID agentId;
    private AgentModel agent;
    private UUID channelConnectionId;
    private ChannelConnectionModel channelConnection;
    private boolean enabled;
    private String config;
    private Instant createdAt;

    public UUID getId() {
        return id;
    }

    public UUID getAgentId() {
        return agentId;
    }

    public AgentModel getAgent() {
        return agent;
    }

    public UUID getChannelConnectionId() {
        return channelConnectionId;
    }

    public ChannelConnectionModel getChannelConnection() {
        return channelConnection;
    }

    public boolean getEnabled() {
        return enabled;
    }

    public String getConfig() {
        return config;
    }

    public Instant getCreatedAt() {
        return createdAt;
    }
}
