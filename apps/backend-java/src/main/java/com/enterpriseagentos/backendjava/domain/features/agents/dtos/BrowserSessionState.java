package com.enterpriseagentos.backendjava.domain.features.agents.dtos;

import java.time.Instant;
import java.util.UUID;

public final class BrowserSessionState  {
    private final UUID agentId;
    private final String runtimeSessionId;
    private final String status;
    private final String name;
    private final String currentUrl;
    private final String title;
    private final String takeoverUrl;
    private final Instant createdAt;
    private final Instant lastAccessedAt;

    public BrowserSessionState(UUID agentId, String runtimeSessionId, String status, String name, String currentUrl, String title, String takeoverUrl, Instant createdAt, Instant lastAccessedAt) {
        this.agentId = agentId;
        this.runtimeSessionId = runtimeSessionId;
        this.status = status;
        this.name = name;
        this.currentUrl = currentUrl;
        this.title = title;
        this.takeoverUrl = takeoverUrl;
        this.createdAt = createdAt;
        this.lastAccessedAt = lastAccessedAt;
    }

    public UUID getAgentId() {
        return agentId;
}

    public UUID agentId() {
        return agentId;
    }

    public String getRuntimeSessionId() {
        return runtimeSessionId;
}

    public String runtimeSessionId() {
        return runtimeSessionId;
    }

    public String getStatus() {
        return status;
}

    public String status() {
        return status;
    }

    public String getName() {
        return name;
}

    public String name() {
        return name;
    }

    public String getCurrentUrl() {
        return currentUrl;
}

    public String currentUrl() {
        return currentUrl;
    }

    public String getTitle() {
        return title;
}

    public String title() {
        return title;
    }

    public String getTakeoverUrl() {
        return takeoverUrl;
}

    public String takeoverUrl() {
        return takeoverUrl;
    }

    public Instant getCreatedAt() {
        return createdAt;
}

    public Instant createdAt() {
        return createdAt;
    }

    public Instant getLastAccessedAt() {
        return lastAccessedAt;
}

    public Instant lastAccessedAt() {
        return lastAccessedAt;
    }
}
