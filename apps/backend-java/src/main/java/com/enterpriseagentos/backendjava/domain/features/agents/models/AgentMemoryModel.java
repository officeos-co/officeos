package com.enterpriseagentos.backendjava.domain.features.agents.models;

import java.time.Instant;
import java.util.UUID;

public class AgentMemoryModel {
    private UUID id = UUID.randomUUID();
    private UUID agentId;
    private String key;
    private String content;
    private Instant createdAt = Instant.now();
    private Instant updatedAt = Instant.now();
    private AgentModel agent;

    public static AgentMemoryModel create(UUID agentId, String key, String content) {
        validateKey(key);
        validateContent(content);
        AgentMemoryModel model = new AgentMemoryModel();
        model.agentId = agentId;
        model.key = key.trim();
        model.content = content;
        return model;
    }

    public String formatPromptSection() {
        return "### " + key + "\n" + content;
    }

    public void updateContent(String content) {
        validateContent(content);
        this.content = content;
        this.updatedAt = Instant.now();
    }

    private static void validateKey(String key) {
        if (key == null || key.isBlank()) {
            throw new IllegalArgumentException("Memory key must not be empty.");
        }
        if (key.length() > 512) {
            throw new IllegalArgumentException("Memory key must not exceed 512 characters.");
        }
    }

    private static void validateContent(String content) {
        if (content == null) {
            throw new IllegalArgumentException("Memory content must not be null.");
        }
    }

    public UUID getId() {
        return id;
    }

    public UUID getAgentId() {
        return agentId;
    }

    public String getKey() {
        return key;
    }

    public String getContent() {
        return content;
    }

    public Instant getCreatedAt() {
        return createdAt;
    }

    public Instant getUpdatedAt() {
        return updatedAt;
    }

    public AgentModel getAgent() {
        return agent;
    }
}
