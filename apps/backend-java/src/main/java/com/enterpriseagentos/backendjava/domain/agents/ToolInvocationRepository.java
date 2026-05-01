package com.enterpriseagentos.backendjava.domain.agents;

import java.util.List;
import java.util.UUID;

public interface ToolInvocationRepository {
    List<ToolInvocation> findByAgentId(UUID agentId);

    ToolInvocation save(ToolInvocation invocation);
}
