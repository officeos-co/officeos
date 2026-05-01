package com.enterpriseagentos.backendjava.domain.features.agents.dtos;

public final class UpdateAgentChannelBindingRequest  {
    private final boolean enabled;
    private final AgentChannelConfig config;

    public UpdateAgentChannelBindingRequest(boolean enabled, AgentChannelConfig config) {
        this.enabled = enabled;
        this.config = config;
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
}
