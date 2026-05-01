package com.enterpriseagentos.backendjava.infrastructure.persistence.agents;

import com.enterpriseagentos.backendjava.domain.agents.AgentStatus;
import jakarta.persistence.Column;
import jakarta.persistence.Entity;
import jakarta.persistence.EnumType;
import jakarta.persistence.Enumerated;
import jakarta.persistence.Id;
import jakarta.persistence.Table;
import java.time.OffsetDateTime;
import java.util.UUID;

@Entity
@Table(name = "agents")
public class AgentEntity {
    @Id
    private UUID id;

    @Column(nullable = false, length = 120)
    private String name;

    @Enumerated(EnumType.STRING)
    @Column(nullable = false, length = 32)
    private AgentStatus status;

    @Column(nullable = false)
    private OffsetDateTime createdAt;

    protected AgentEntity() {
    }

    public AgentEntity(UUID id, String name, AgentStatus status, OffsetDateTime createdAt) {
        this.id = id;
        this.name = name;
        this.status = status;
        this.createdAt = createdAt;
    }

    public UUID id() {
        return id;
    }

    public String name() {
        return name;
    }

    public AgentStatus status() {
        return status;
    }

    public OffsetDateTime createdAt() {
        return createdAt;
    }
}
