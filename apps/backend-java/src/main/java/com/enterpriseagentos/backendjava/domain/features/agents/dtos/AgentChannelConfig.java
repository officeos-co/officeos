package com.enterpriseagentos.backendjava.domain.features.agents.dtos;

public final class AgentChannelConfig  {
    private final String platformId;
    private final String threadId;

    public AgentChannelConfig(String platformId, String threadId) {
        this.platformId = platformId;
        this.threadId = threadId;
    }

    public String getPlatformId() {
        return platformId;
}

    public String platformId() {
        return platformId;
    }

    public String getThreadId() {
        return threadId;
}

    public String threadId() {
        return threadId;
    }
}
