package com.enterpriseagentos.backendjava.domain.features.agents.dtos;

import java.util.List;

public final class AgentInitRequest  {
    private final List<String> toolNames;
    private final List<AgentToolPermissionInit> toolPermissions;
    private final List<String> channelSlugs;
    private final String bootstrapMessage;

    public AgentInitRequest(List<String> toolNames, List<AgentToolPermissionInit> toolPermissions, List<String> channelSlugs, String bootstrapMessage) {
        this.toolNames = toolNames;
        this.toolPermissions = toolPermissions;
        this.channelSlugs = channelSlugs;
        this.bootstrapMessage = bootstrapMessage;
    }

    public List<String> getToolNames() {
        return toolNames;
}

    public List<String> toolNames() {
        return toolNames;
    }

    public List<AgentToolPermissionInit> getToolPermissions() {
        return toolPermissions;
}

    public List<AgentToolPermissionInit> toolPermissions() {
        return toolPermissions;
    }

    public List<String> getChannelSlugs() {
        return channelSlugs;
}

    public List<String> channelSlugs() {
        return channelSlugs;
    }

    public String getBootstrapMessage() {
        return bootstrapMessage;
}

    public String bootstrapMessage() {
        return bootstrapMessage;
    }
}
