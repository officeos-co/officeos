package com.enterpriseagentos.backendjava.domain.features.agents.models;

import java.time.Instant;
import java.util.UUID;

public class AgentSessionContextModel {
    private UUID id;
    private UUID agentId;
    private String summary;
    private UUID lastCompactedLogId;
    private Instant lastCompactedAt;
    private int preCompactTokens;
    private int postCompactTokens;
    private int compactionVersion;
    private Instant createdAt;
    private Instant updatedAt;

    public UUID getId() {
        return id;
    }

    public UUID getAgentId() {
        return agentId;
    }

    public String getSummary() {
        return summary;
    }

    public UUID getLastCompactedLogId() {
        return lastCompactedLogId;
    }

    public Instant getLastCompactedAt() {
        return lastCompactedAt;
    }

    public int getPreCompactTokens() {
        return preCompactTokens;
    }

    public int getPostCompactTokens() {
        return postCompactTokens;
    }

    public int getCompactionVersion() {
        return compactionVersion;
    }

    public Instant getCreatedAt() {
        return createdAt;
    }

    public Instant getUpdatedAt() {
        return updatedAt;
    }
}
