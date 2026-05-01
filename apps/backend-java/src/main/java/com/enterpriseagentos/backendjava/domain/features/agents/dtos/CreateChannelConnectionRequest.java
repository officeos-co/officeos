package com.enterpriseagentos.backendjava.domain.features.agents.dtos;

import java.util.Map;

public final class CreateChannelConnectionRequest  {
    private final String channelType;
    private final String displayName;
    private final Map<String, String> config;

    public CreateChannelConnectionRequest(String channelType, String displayName, Map<String, String> config) {
        this.channelType = channelType;
        this.displayName = displayName;
        this.config = config;
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

    public Map<String, String> getConfig() {
        return config;
}

    public Map<String, String> config() {
        return config;
    }
}
