package com.enterpriseagentos.backendjava.domain.features.agents.dtos;

import java.time.Instant;
import java.util.UUID;

public final class ProviderDto  {
    private final UUID id;
    private final String name;
    private final String displayName;
    private final boolean configured;
    private final Instant configuredAt;

    public ProviderDto(UUID id, String name, String displayName, boolean configured, Instant configuredAt) {
        this.id = id;
        this.name = name;
        this.displayName = displayName;
        this.configured = configured;
        this.configuredAt = configuredAt;
    }

    public UUID getId() {
        return id;
}

    public UUID id() {
        return id;
    }

    public String getName() {
        return name;
}

    public String name() {
        return name;
    }

    public String getDisplayName() {
        return displayName;
}

    public String displayName() {
        return displayName;
    }

    public boolean getConfigured() {
        return configured;
}

    public boolean configured() {
        return configured;
    }

    public Instant getConfiguredAt() {
        return configuredAt;
}

    public Instant configuredAt() {
        return configuredAt;
    }
}
