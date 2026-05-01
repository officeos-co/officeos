package com.enterpriseagentos.backendjava.domain.features.agents.dtos;

import java.util.Map;

public final class UpdateChannelConnectionRequest  {
    private final String displayName;
    private final boolean enabled;
    private final Map<String, String> config;

    public UpdateChannelConnectionRequest(String displayName, boolean enabled, Map<String, String> config) {
        this.displayName = displayName;
        this.enabled = enabled;
        this.config = config;
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

    public Map<String, String> getConfig() {
        return config;
}

    public Map<String, String> config() {
        return config;
    }
}
