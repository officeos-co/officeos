package com.enterpriseagentos.backendjava.domain.features.agents.models;

import java.time.Instant;
import java.util.List;
import java.util.Locale;
import java.util.UUID;

import com.enterpriseagentos.backendjava.domain.common.services.ProviderRegistry;
import com.enterpriseagentos.backendjava.domain.common.valueobjects.AgentStatus;

public class AgentModel {
    private UUID id = UUID.randomUUID();
    private String name;
    private String provider;
    private String model;
    private AgentStatus status = AgentStatus.Pending;
    private String podName;
    private String serviceUrl;
    private String prompt;
    private Instant createdAt = Instant.now();
    private boolean isDeleted;
    private UUID ownerId;
    private String encryptedBackendToken;
    private List<AgentPersonalityModel> personalityFiles = List.of();
    private List<AgentMemoryModel> memories = List.of();
    private List<AgentCronJobModel> cronJobs = List.of();
    private List<AgentRateLimitModel> rateLimits = List.of();
    private List<AgentChannelBindingModel> channelBindings = List.of();
    private AgentSessionModel activeSession;

    private AgentModel() {
    }

    public static AgentModel create(String name, String provider, String model, UUID ownerId, String prompt) {
        if (name == null || name.isBlank()) {
            throw new IllegalArgumentException("Agent name is required.");
        }
        if (provider == null || provider.isBlank()) {
            throw new IllegalArgumentException("Provider is required.");
        }

        AgentModel agent = new AgentModel();
        agent.name = name.trim();
        agent.provider = provider.trim().toLowerCase(Locale.ROOT);
        agent.ownerId = ownerId;
        agent.prompt = prompt == null || prompt.isBlank() ? null : prompt;
        agent.validateAndSetModel(model);
        return agent;
    }

    public boolean hasPod() {
        return podName != null && !podName.isBlank();
    }

    public void markDeployed(String podName, String serviceUrl) {
        this.podName = podName;
        this.serviceUrl = serviceUrl;
        this.status = AgentStatus.Running;
    }

    public void markFailed() {
        this.status = AgentStatus.Failed;
    }

    public void validateAndSetModel(String model) {
        if (model == null || model.isBlank()) {
            this.model = ProviderRegistry.DEFAULT_MODEL;
            return;
        }

        String trimmed = model.trim();
        if (!ProviderRegistry.isValidModel(trimmed)) {
            throw new IllegalStateException(
                "Model '" + trimmed + "' is not a known model. Allowed: " + String.join(", ", ProviderRegistry.SUPPORTED_MODELS)
            );
        }

        this.model = trimmed;
    }

    public UUID getId() {
        return id;
    }

    public String getName() {
        return name;
    }

    public String getProvider() {
        return provider;
    }

    public String getModel() {
        return model;
    }

    public AgentStatus getStatus() {
        return status;
    }

    public String getPodName() {
        return podName;
    }

    public String getServiceUrl() {
        return serviceUrl;
    }

    public String getPrompt() {
        return prompt;
    }

    public Instant getCreatedAt() {
        return createdAt;
    }

    public boolean getIsDeleted() {
        return isDeleted;
    }

    public UUID getOwnerId() {
        return ownerId;
    }

    public String getEncryptedBackendToken() {
        return encryptedBackendToken;
    }

    public List<AgentPersonalityModel> getPersonalityFiles() {
        return personalityFiles;
    }

    public List<AgentMemoryModel> getMemories() {
        return memories;
    }

    public List<AgentCronJobModel> getCronJobs() {
        return cronJobs;
    }

    public List<AgentRateLimitModel> getRateLimits() {
        return rateLimits;
    }

    public List<AgentChannelBindingModel> getChannelBindings() {
        return channelBindings;
    }

    public AgentSessionModel getActiveSession() {
        return activeSession;
    }
}
