package com.enterpriseagentos.backendjava.domain.features.agents;

import java.util.Map;
import java.util.UUID;

public record AgentSandboxCreateRequest(UUID agentId, AgentTemplateRecord template, Map<String, String> environment, Map<String, String> metadata) {
}
