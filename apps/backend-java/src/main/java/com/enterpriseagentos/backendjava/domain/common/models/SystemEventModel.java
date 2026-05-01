package com.enterpriseagentos.backendjava.domain.common.models;

import java.time.Instant;
import java.util.UUID;

public class SystemEventModel {
    private UUID id;
    private String severity;
    private String category;
    private String message;
    private String detailJson;
    private String skillName;
    private UUID agentId;
    private String correlationId;
    private boolean acknowledged;
    private Instant createdAt;

    public UUID getId() {
        return id;
    }

    public String getSeverity() {
        return severity;
    }

    public String getCategory() {
        return category;
    }

    public String getMessage() {
        return message;
    }

    public String getDetailJson() {
        return detailJson;
    }

    public String getSkillName() {
        return skillName;
    }

    public UUID getAgentId() {
        return agentId;
    }

    public String getCorrelationId() {
        return correlationId;
    }

    public boolean getAcknowledged() {
        return acknowledged;
    }

    public Instant getCreatedAt() {
        return createdAt;
    }
}
