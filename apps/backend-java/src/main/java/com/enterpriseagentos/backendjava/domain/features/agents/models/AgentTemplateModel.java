package com.enterpriseagentos.backendjava.domain.features.agents.models;

import java.time.Instant;
import java.util.UUID;

import com.enterpriseagentos.backendjava.domain.features.management.models.UserModel;

public class AgentTemplateModel {
    private UUID id;
    private String name;
    private String description;
    private String prompt;
    private String integrationsJson;
    private String channelsJson;
    private boolean isBuiltin;
    private UUID ownerId;
    private UserModel owner;
    private Instant createdAt;

    private AgentTemplateModel() {
    }

    public static AgentTemplateModel builtIn(
        UUID id,
        String name,
        String description,
        String integrationsJson,
        String channelsJson,
        String prompt
    ) {
        AgentTemplateModel model = new AgentTemplateModel();
        model.id = id;
        model.name = name;
        model.description = description;
        model.integrationsJson = integrationsJson;
        model.channelsJson = channelsJson;
        model.prompt = prompt;
        model.isBuiltin = true;
        model.createdAt = Instant.now();
        return model;
    }

    public UUID getId() {
        return id;
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

    public String getIntegrationsJson() {
        return integrationsJson;
    }

    public String getChannelsJson() {
        return channelsJson;
    }

    public boolean getIsBuiltin() {
        return isBuiltin;
    }

    public UUID getOwnerId() {
        return ownerId;
    }

    public UserModel getOwner() {
        return owner;
    }

    public Instant getCreatedAt() {
        return createdAt;
    }
}
