package com.enterpriseagentos.backendjava.domain.events;

import java.util.UUID;

import com.enterpriseagentos.backendjava.domain.features.analytics.enums.AgentLogType;

public final class ChannelMessageRoutedEvent implements DomainEvent {
    private final UUID agentId;
    private final AgentLogType logType;
    private final String channelType;
    private final String content;
    private final String correlationId;

    public ChannelMessageRoutedEvent(UUID agentId, AgentLogType logType, String channelType, String content, String correlationId) {
        this.agentId = agentId;
        this.logType = logType;
        this.channelType = channelType;
        this.content = content;
        this.correlationId = correlationId;
    }

    public UUID getAgentId() {
        return agentId;
}

    public UUID agentId() {
        return agentId;
    }

    public AgentLogType getLogType() {
        return logType;
}

    public AgentLogType logType() {
        return logType;
    }

    public String getChannelType() {
        return channelType;
}

    public String channelType() {
        return channelType;
    }

    public String getContent() {
        return content;
}

    public String content() {
        return content;
    }

    public String getCorrelationId() {
        return correlationId;
}

    public String correlationId() {
        return correlationId;
    }
}
