package com.enterpriseagentos.backendjava.domain.agents;

import java.time.OffsetDateTime;
import java.util.UUID;

public record ToolInvocation(
    UUID id,
    UUID agentId,
    String toolName,
    ToolInvocationStatus status,
    String failureReason,
    OffsetDateTime createdAt
) {
}
