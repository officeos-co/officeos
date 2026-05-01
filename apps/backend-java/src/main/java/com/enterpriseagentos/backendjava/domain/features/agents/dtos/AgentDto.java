package com.enterpriseagentos.backendjava.domain.features.agents.dtos;

import java.time.Instant;
import java.util.UUID;

public final class AgentDto  {
    private final UUID id;
    private final String name;
    private final String provider;
    private final String model;
    private final String prompt;
    private final String status;
    private final String podName;
    private final String serviceUrl;
    private final Instant createdAt;

    public AgentDto(UUID id, String name, String provider, String model, String prompt, String status, String podName, String serviceUrl, Instant createdAt) {
        this.id = id;
        this.name = name;
        this.provider = provider;
        this.model = model;
        this.prompt = prompt;
        this.status = status;
        this.podName = podName;
        this.serviceUrl = serviceUrl;
        this.createdAt = createdAt;
    }

    public UUID getId() {
        return id;
}

    public UUID id() {
        return id;
    }

    public String getName() {
        return name;
}

    public String name() {
        return name;
    }

    public String getProvider() {
        return provider;
}

    public String provider() {
        return provider;
    }

    public String getModel() {
        return model;
}

    public String model() {
        return model;
    }

    public String getPrompt() {
        return prompt;
}

    public String prompt() {
        return prompt;
    }

    public String getStatus() {
        return status;
}

    public String status() {
        return status;
    }

    public String getPodName() {
        return podName;
}

    public String podName() {
        return podName;
    }

    public String getServiceUrl() {
        return serviceUrl;
}

    public String serviceUrl() {
        return serviceUrl;
    }

    public Instant getCreatedAt() {
        return createdAt;
}

    public Instant createdAt() {
        return createdAt;
    }
}
