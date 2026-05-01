package com.enterpriseagentos.backendjava.domain.features.agents.models;

import java.time.Instant;
import java.util.UUID;

public class BrowserSessionModel {
    private UUID id;
    private UUID agentId;
    private String runtimeSessionId;
    private String cookiesJson;
    private Instant createdAt;
    private Instant lastAccessedAt;

    public UUID getId() {
        return id;
    }

    public UUID getAgentId() {
        return agentId;
    }

    public String getRuntimeSessionId() {
        return runtimeSessionId;
    }

    public String getCookiesJson() {
        return cookiesJson;
    }

    public Instant getCreatedAt() {
        return createdAt;
    }

    public Instant getLastAccessedAt() {
        return lastAccessedAt;
    }
}
