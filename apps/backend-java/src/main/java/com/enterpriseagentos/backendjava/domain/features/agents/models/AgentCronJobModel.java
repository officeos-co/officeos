package com.enterpriseagentos.backendjava.domain.features.agents.models;

import java.time.Instant;
import java.util.UUID;

import com.enterpriseagentos.backendjava.domain.common.valueobjects.CronExpression;

public class AgentCronJobModel {
    private UUID id;
    private UUID agentId;
    private String name;
    private CronExpression expression;
    private String prompt;
    private boolean enabled;
    private Instant lastRunAt;
    private Instant nextRunAt;
    private Instant createdAt;

    public UUID getId() {
        return id;
    }

    public UUID getAgentId() {
        return agentId;
    }

    public String getName() {
        return name;
    }

    public CronExpression getExpression() {
        return expression;
    }

    public String getPrompt() {
        return prompt;
    }

    public boolean getEnabled() {
        return enabled;
    }

    public Instant getLastRunAt() {
        return lastRunAt;
    }

    public Instant getNextRunAt() {
        return nextRunAt;
    }

    public Instant getCreatedAt() {
        return createdAt;
    }
}
