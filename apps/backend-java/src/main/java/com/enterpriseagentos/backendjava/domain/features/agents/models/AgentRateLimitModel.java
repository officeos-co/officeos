package com.enterpriseagentos.backendjava.domain.features.agents.models;

import java.time.Instant;
import java.util.UUID;

public class AgentRateLimitModel {
    private UUID id;
    private UUID agentId;
    private String bucketKey;
    private Instant windowStart;
    private int count;

    public UUID getId() {
        return id;
    }

    public UUID getAgentId() {
        return agentId;
    }

    public String getBucketKey() {
        return bucketKey;
    }

    public Instant getWindowStart() {
        return windowStart;
    }

    public int getCount() {
        return count;
    }
}
