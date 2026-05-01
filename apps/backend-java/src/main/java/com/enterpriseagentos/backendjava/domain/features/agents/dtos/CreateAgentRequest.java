package com.enterpriseagentos.backendjava.domain.features.agents.dtos;

public final class CreateAgentRequest  {
    private final String name;
    private final String provider;
    private final String model;
    private final String prompt;

    public CreateAgentRequest(String name, String provider, String model, String prompt) {
        this.name = name;
        this.provider = provider;
        this.model = model;
        this.prompt = prompt;
    }

    public String getName() {
        return name;
}

    public String name() {
        return name;
    }

    public String getProvider() {
        return provider;
}

    public String provider() {
        return provider;
    }

    public String getModel() {
        return model;
}

    public String model() {
        return model;
    }

    public String getPrompt() {
        return prompt;
}

    public String prompt() {
        return prompt;
    }
}
