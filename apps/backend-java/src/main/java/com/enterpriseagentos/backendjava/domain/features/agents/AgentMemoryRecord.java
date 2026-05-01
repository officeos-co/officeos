package com.enterpriseagentos.backendjava.domain.features.agents;

import java.time.Instant;
import java.util.UUID;

public class AgentMemoryRecord {
    public UUID id = UUID.randomUUID();
    public UUID agentId;
    public String key;
    public String content;
    public Instant createdAt = Instant.now();
    public Instant updatedAt = Instant.now();
    public AgentRecord agent;

    public static AgentMemoryRecord create(UUID agentId, String key, String content) {
        validateKey(key);
        validateContent(content);
        AgentMemoryRecord record = new AgentMemoryRecord();
        record.agentId = agentId;
        record.key = key.trim();
        record.content = content;
        return record;
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
}
