package com.enterpriseagentos.backendjava.domain.agents;

import org.springframework.stereotype.Component;

@Component
public class AgentNamePolicy {
    public String normalize(String name) {
        return name == null ? "" : name.trim();
    }

    public boolean isValid(String name) {
        var normalized = normalize(name);
        return !normalized.isBlank() && normalized.length() <= 120;
    }
}
