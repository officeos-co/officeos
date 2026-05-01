package com.enterpriseagentos.backendjava.infrastructure.persistence.toolinvocations;

import com.enterpriseagentos.backendjava.domain.agents.ToolInvocation;

final class ToolInvocationMapper {
    private ToolInvocationMapper() {
    }

    static ToolInvocation toDomain(ToolInvocationEntity entity) {
        return new ToolInvocation(
            entity.id(),
            entity.agentId(),
            entity.toolName(),
            entity.status(),
            entity.failureReason(),
            entity.createdAt()
        );
    }

    static ToolInvocationEntity toEntity(ToolInvocation invocation) {
        return new ToolInvocationEntity(
            invocation.id(),
            invocation.agentId(),
            invocation.toolName(),
            invocation.status(),
            invocation.failureReason(),
            invocation.createdAt()
        );
    }
}
