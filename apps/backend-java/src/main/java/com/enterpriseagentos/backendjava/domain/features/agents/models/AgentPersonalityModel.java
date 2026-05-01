package com.enterpriseagentos.backendjava.domain.features.agents.models;

import java.time.Instant;
import java.util.List;
import java.util.UUID;

import com.enterpriseagentos.backendjava.domain.common.valueobjects.PersonalityFileName;

public class AgentPersonalityModel {
    public static final List<String> ORDERED_FILE_NAMES = PersonalityFileName.KNOWN_FILE_NAMES;

    private UUID id = UUID.randomUUID();
    private UUID agentId;
    private String fileName;
    private String content;
    private Instant createdAt = Instant.now();
    private Instant updatedAt = Instant.now();
    private AgentModel agent;

    public static AgentPersonalityModel create(UUID agentId, String fileName, String content) {
        validateFileName(fileName);
        validateContent(content);
        AgentPersonalityModel model = new AgentPersonalityModel();
        model.agentId = agentId;
        model.fileName = fileName.trim();
        model.content = content;
        return model;
    }

    public void updateContent(String content) {
        validateContent(content);
        this.content = content;
        this.updatedAt = Instant.now();
    }

    public String formatPromptSection() {
        return "<file path=\"" + fileName + "\">\n" + content.trim() + "\n</file>";
    }

    public int compositionOrder() {
        for (int index = 0; index < ORDERED_FILE_NAMES.size(); index++) {
            if (ORDERED_FILE_NAMES.get(index).equalsIgnoreCase(fileName)) {
                return index;
            }
        }
        return ORDERED_FILE_NAMES.size() + 1;
    }

    private static void validateFileName(String fileName) {
        new PersonalityFileName(fileName);
    }

    private static void validateContent(String content) {
        if (content == null) {
            throw new IllegalArgumentException("Personality content must not be null.");
        }
    }

    public UUID getId() {
        return id;
    }

    public UUID getAgentId() {
        return agentId;
    }

    public String getFileName() {
        return fileName;
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
