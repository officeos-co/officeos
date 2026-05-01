package com.enterpriseagentos.backendjava.domain.features.agents.dtos;

public final class AgentDeployment  {
    private final String podName;
    private final String serviceUrl;

    public AgentDeployment(String podName, String serviceUrl) {
        this.podName = podName;
        this.serviceUrl = serviceUrl;
    }

    public String getPodName() {
        return podName;
}

    public String podName() {
        return podName;
    }

    public String getServiceUrl() {
        return serviceUrl;
}

    public String serviceUrl() {
        return serviceUrl;
    }
}
