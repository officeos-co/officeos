package com.enterpriseagentos.backendjava.domain.features.agents.dtos;

import java.util.Map;
import java.util.UUID;

import com.enterpriseagentos.backendjava.domain.features.agents.models.AgentTemplateModel;

public final class AgentSandboxCreateRequest  {
    private final UUID agentId;
    private final AgentTemplateModel template;
    private final Map<String, String> environment;
    private final Map<String, String> metadata;

    public AgentSandboxCreateRequest(UUID agentId, AgentTemplateModel template, Map<String, String> environment, Map<String, String> metadata) {
        this.agentId = agentId;
        this.template = template;
        this.environment = environment;
        this.metadata = metadata;
    }

    public UUID getAgentId() {
        return agentId;
}

    public UUID agentId() {
        return agentId;
    }

    public AgentTemplateModel getTemplate() {
        return template;
}

    public AgentTemplateModel template() {
        return template;
    }

    public Map<String, String> getEnvironment() {
        return environment;
}

    public Map<String, String> environment() {
        return environment;
    }

    public Map<String, String> getMetadata() {
        return metadata;
}

    public Map<String, String> metadata() {
        return metadata;
    }
}
