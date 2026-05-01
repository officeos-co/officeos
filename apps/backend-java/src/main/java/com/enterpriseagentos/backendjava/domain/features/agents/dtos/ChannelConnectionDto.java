package com.enterpriseagentos.backendjava.domain.features.agents.dtos;

import java.time.Instant;
import java.util.UUID;

public final class ChannelConnectionDto  {
    private final UUID id;
    private final String channelType;
    private final String displayName;
    private final boolean enabled;
    private final boolean configured;
    private final Instant createdAt;
    private final UUID createdById;

    public ChannelConnectionDto(UUID id, String channelType, String displayName, boolean enabled, boolean configured, Instant createdAt, UUID createdById) {
        this.id = id;
        this.channelType = channelType;
        this.displayName = displayName;
        this.enabled = enabled;
        this.configured = configured;
        this.createdAt = createdAt;
        this.createdById = createdById;
    }

    public UUID getId() {
        return id;
}

    public UUID id() {
        return id;
    }

    public String getChannelType() {
        return channelType;
}

    public String channelType() {
        return channelType;
    }

    public String getDisplayName() {
        return displayName;
}

    public String displayName() {
        return displayName;
    }

    public boolean getEnabled() {
        return enabled;
}

    public boolean enabled() {
        return enabled;
    }

    public boolean getConfigured() {
        return configured;
}

    public boolean configured() {
        return configured;
    }

    public Instant getCreatedAt() {
        return createdAt;
}

    public Instant createdAt() {
        return createdAt;
    }

    public UUID getCreatedById() {
        return createdById;
}

    public UUID createdById() {
        return createdById;
    }
}
