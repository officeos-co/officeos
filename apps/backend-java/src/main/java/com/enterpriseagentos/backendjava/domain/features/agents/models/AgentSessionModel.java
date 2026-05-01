package com.enterpriseagentos.backendjava.domain.features.agents.models;

import java.time.Instant;
import java.util.UUID;

import com.enterpriseagentos.backendjava.domain.common.valueobjects.SessionStatus;


public class AgentSessionModel {
    private UUID id;
    private UUID agentId;
    private SessionStatus status;
    private int messageCount;
    private Instant lastActivityAt;
    private Instant createdAt;
    private Instant endedAt;
    private AgentModel agent;

    public UUID getId() {
        return id;
    }

    public UUID getAgentId() {
        return agentId;
    }

    public SessionStatus getStatus() {
        return status;
    }

    public int getMessageCount() {
        return messageCount;
    }

    public Instant getLastActivityAt() {
        return lastActivityAt;
    }

    public Instant getCreatedAt() {
        return createdAt;
    }

    public Instant getEndedAt() {
        return endedAt;
    }

    public AgentModel getAgent() {
        return agent;
    }
}
