package com.enterpriseagentos.backendjava.domain.features.agents;

import java.time.Instant;
import java.util.List;
import java.util.UUID;
import com.enterpriseagentos.backendjava.domain.common.valueobjects.PersonalityFileName;

public class AgentPersonalityRecord {
    public static final List<String> ORDERED_FILE_NAMES = PersonalityFileName.KNOWN_FILE_NAMES;

    public UUID id = UUID.randomUUID();
    public UUID agentId;
    public String fileName;
    public String content;
    public Instant createdAt = Instant.now();
    public Instant updatedAt = Instant.now();
    public AgentRecord agent;

    public static AgentPersonalityRecord create(UUID agentId, String fileName, String content) {
        validateFileName(fileName);
        validateContent(content);
        AgentPersonalityRecord record = new AgentPersonalityRecord();
        record.agentId = agentId;
        record.fileName = fileName.trim();
        record.content = content;
        return record;
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
}
