package com.enterpriseagentos.backendjava.domain.agents;

import java.time.OffsetDateTime;
import java.util.UUID;

public record Agent(UUID id, String name, AgentStatus status, OffsetDateTime createdAt) {
}
