package com.enterpriseagentos.backendjava.domain.features.agents.models;

import java.time.Instant;
import java.util.UUID;

public class AgentRunModel {
    private UUID id;
    private UUID agentId;
    private UUID parentRunId;
    private String parentCorrelationId;
    private String kind;
    private String status;
    private String name;
    private String description;
    private String prompt;
    private String result;
    private String error;
    private Instant createdAt;
    private Instant updatedAt;
    private Instant completedAt;

    public UUID getId() {
        return id;
    }

    public UUID getAgentId() {
        return agentId;
    }

    public UUID getParentRunId() {
        return parentRunId;
    }

    public String getParentCorrelationId() {
        return parentCorrelationId;
    }

    public String getKind() {
        return kind;
    }

    public String getStatus() {
        return status;
    }

    public String getName() {
        return name;
    }

    public String getDescription() {
        return description;
    }

    public String getPrompt() {
        return prompt;
    }

    public String getResult() {
        return result;
    }

    public String getError() {
        return error;
    }

    public Instant getCreatedAt() {
        return createdAt;
    }

    public Instant getUpdatedAt() {
        return updatedAt;
    }

    public Instant getCompletedAt() {
        return completedAt;
    }
}
