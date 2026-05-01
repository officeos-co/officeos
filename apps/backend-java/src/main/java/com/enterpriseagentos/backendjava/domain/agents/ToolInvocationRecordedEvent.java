package com.enterpriseagentos.backendjava.domain.agents;

import java.util.UUID;

public record ToolInvocationRecordedEvent(UUID toolInvocationId, UUID agentId, ToolInvocationStatus status) {
}
