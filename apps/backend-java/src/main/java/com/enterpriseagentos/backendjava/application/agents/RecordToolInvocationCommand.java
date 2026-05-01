package com.enterpriseagentos.backendjava.application.agents;

import com.enterpriseagentos.backendjava.domain.agents.ToolInvocationStatus;
import java.util.UUID;

public record RecordToolInvocationCommand(
    UUID agentId,
    String toolName,
    ToolInvocationStatus status,
    String failureReason
) {
}
