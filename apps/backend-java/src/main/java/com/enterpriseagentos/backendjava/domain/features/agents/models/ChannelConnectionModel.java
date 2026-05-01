package com.enterpriseagentos.backendjava.domain.features.agents.models;

import java.time.Instant;
import java.util.List;
import java.util.UUID;

import com.enterpriseagentos.backendjava.domain.common.valueobjects.ChannelType;
import com.enterpriseagentos.backendjava.domain.features.management.models.UserModel;

public class ChannelConnectionModel {
    private UUID id;
    private ChannelType channelType;
    private String displayName;
    private boolean enabled;
    private Instant createdAt;
    private UUID createdById;
    private String encryptedCreds;
    private UserModel createdBy;
    private List<AgentChannelBindingModel> bindings;

    public UUID getId() {
        return id;
    }

    public ChannelType getChannelType() {
        return channelType;
    }

    public String getDisplayName() {
        return displayName;
    }

    public boolean getEnabled() {
        return enabled;
    }

    public Instant getCreatedAt() {
        return createdAt;
    }

    public UUID getCreatedById() {
        return createdById;
    }

    public String getEncryptedCreds() {
        return encryptedCreds;
    }

    public UserModel getCreatedBy() {
        return createdBy;
    }

    public List<AgentChannelBindingModel> getBindings() {
        return bindings;
    }
}
