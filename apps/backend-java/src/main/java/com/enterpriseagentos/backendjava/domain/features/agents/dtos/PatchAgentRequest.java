package com.enterpriseagentos.backendjava.domain.features.agents.dtos;

public final class PatchAgentRequest  {
    private final String provider;
    private final String model;
    private final String name;
    private final String prompt;

    public PatchAgentRequest(String provider, String model, String name, String prompt) {
        this.provider = provider;
        this.model = model;
        this.name = name;
        this.prompt = prompt;
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

    public String getName() {
        return name;
}

    public String name() {
        return name;
    }

    public String getPrompt() {
        return prompt;
}

    public String prompt() {
        return prompt;
    }
}
