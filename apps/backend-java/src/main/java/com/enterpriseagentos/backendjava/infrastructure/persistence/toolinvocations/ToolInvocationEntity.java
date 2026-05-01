package com.enterpriseagentos.backendjava.infrastructure.persistence.toolinvocations;

import com.enterpriseagentos.backendjava.domain.agents.ToolInvocationStatus;
import jakarta.persistence.Column;
import jakarta.persistence.Entity;
import jakarta.persistence.EnumType;
import jakarta.persistence.Enumerated;
import jakarta.persistence.Id;
import jakarta.persistence.Table;
import java.time.OffsetDateTime;
import java.util.UUID;

@Entity
@Table(name = "tool_invocations")
public class ToolInvocationEntity {
    @Id
    private UUID id;

    @Column(nullable = false)
    private UUID agentId;

    @Column(nullable = false, length = 120)
    private String toolName;

    @Enumerated(EnumType.STRING)
    @Column(nullable = false, length = 32)
    private ToolInvocationStatus status;

    @Column(length = 500)
    private String failureReason;

    @Column(nullable = false)
    private OffsetDateTime createdAt;

    protected ToolInvocationEntity() {
    }

    public ToolInvocationEntity(
        UUID id,
        UUID agentId,
        String toolName,
        ToolInvocationStatus status,
        String failureReason,
        OffsetDateTime createdAt
    ) {
        this.id = id;
        this.agentId = agentId;
        this.toolName = toolName;
        this.status = status;
        this.failureReason = failureReason;
        this.createdAt = createdAt;
    }

    public UUID id() {
        return id;
    }

    public UUID agentId() {
        return agentId;
    }

    public String toolName() {
        return toolName;
    }

    public ToolInvocationStatus status() {
        return status;
    }

    public String failureReason() {
        return failureReason;
    }

    public OffsetDateTime createdAt() {
        return createdAt;
    }
}
