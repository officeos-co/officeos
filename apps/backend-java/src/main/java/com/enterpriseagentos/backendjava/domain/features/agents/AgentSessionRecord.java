package com.enterpriseagentos.backendjava.domain.features.agents;

import java.time.Instant;
import java.util.UUID;

import com.enterpriseagentos.backendjava.domain.common.valueobjects.SessionStatus;

public class AgentSessionRecord {
    public UUID id;
    public UUID agentId;
    public SessionStatus status;
    public int messageCount;
    public Instant lastActivityAt;
    public Instant createdAt;
    public Instant endedAt;
    public AgentRecord agent;
}
