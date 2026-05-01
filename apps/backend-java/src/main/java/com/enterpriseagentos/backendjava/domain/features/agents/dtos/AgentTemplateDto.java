package com.enterpriseagentos.backendjava.domain.features.agents.dtos;

import java.util.List;
import java.util.UUID;

public final class AgentTemplateDto  {
    private final UUID id;
    private final String name;
    private final String description;
    private final String prompt;
    private final List<String> integrations;
    private final List<String> channels;
    private final boolean isBuiltin;

    public AgentTemplateDto(UUID id, String name, String description, String prompt, List<String> integrations, List<String> channels, boolean isBuiltin) {
        this.id = id;
        this.name = name;
        this.description = description;
        this.prompt = prompt;
        this.integrations = integrations;
        this.channels = channels;
        this.isBuiltin = isBuiltin;
    }

    public UUID getId() {
        return id;
}

    public UUID id() {
        return id;
    }

    public String getName() {
        return name;
}

    public String name() {
        return name;
    }

    public String getDescription() {
        return description;
}

    public String description() {
        return description;
    }

    public String getPrompt() {
        return prompt;
}

    public String prompt() {
        return prompt;
    }

    public List<String> getIntegrations() {
        return integrations;
}

    public List<String> integrations() {
        return integrations;
    }

    public List<String> getChannels() {
        return channels;
}

    public List<String> channels() {
        return channels;
    }

    public boolean getIsBuiltin() {
        return isBuiltin;
}

    public boolean isBuiltin() {
        return isBuiltin;
    }
}
