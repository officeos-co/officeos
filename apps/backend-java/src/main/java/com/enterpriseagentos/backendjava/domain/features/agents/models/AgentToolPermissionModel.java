package com.enterpriseagentos.backendjava.domain.features.agents.models;

import java.time.Instant;
import java.util.UUID;

import com.enterpriseagentos.backendjava.domain.features.agents.enums.ToolPermission;

public class AgentToolPermissionModel {
    private UUID id;
    private UUID agentId;
    private AgentModel agent;
    private String skillName;
    private String toolName;
    private ToolPermission permission;
    private Instant createdAt;
    private Instant updatedAt;

    public UUID getId() {
        return id;
    }

    public UUID getAgentId() {
        return agentId;
    }

    public AgentModel getAgent() {
        return agent;
    }

    public String getSkillName() {
        return skillName;
    }

    public String getToolName() {
        return toolName;
    }

    public ToolPermission getPermission() {
        return permission;
    }

    public Instant getCreatedAt() {
        return createdAt;
    }

    public Instant getUpdatedAt() {
        return updatedAt;
    }
}
