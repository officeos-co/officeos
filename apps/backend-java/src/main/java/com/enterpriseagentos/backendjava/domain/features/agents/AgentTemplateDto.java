package com.enterpriseagentos.backendjava.domain.features.agents;

import java.util.List;
import java.util.UUID;

public record AgentTemplateDto(UUID id, String name, String description, String prompt, List<String> integrations, List<String> channels, boolean isBuiltin) {
}
