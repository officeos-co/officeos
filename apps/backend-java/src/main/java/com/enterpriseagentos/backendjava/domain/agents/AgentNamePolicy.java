package com.enterpriseagentos.backendjava.domain.agents;

import org.springframework.stereotype.Component;

@Component
public class AgentNamePolicy {
    public boolean isValid(String name) {
        return name != null && !name.isBlank() && name.trim().length() <= 120;
    }

    public String normalize(String name) {
        return name.trim();
    }
}
