package com.enterpriseagentos.backendjava.domain.features.agents.dtos;

public final class AgentSandboxDeployment  {
    private final String sandboxId;
    private final String serviceUrl;

    public AgentSandboxDeployment(String sandboxId, String serviceUrl) {
        this.sandboxId = sandboxId;
        this.serviceUrl = serviceUrl;
    }

    public String getSandboxId() {
        return sandboxId;
}

    public String sandboxId() {
        return sandboxId;
    }

    public String getServiceUrl() {
        return serviceUrl;
}

    public String serviceUrl() {
        return serviceUrl;
    }
}
