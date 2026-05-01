package com.enterpriseagentos.backendjava.domain.features.agents;

import java.time.Instant;
import java.util.UUID;

public record AgentChannelBindingDto(UUID id, UUID agentId, UUID channelConnectionId, String channelType, String channelDisplayName, boolean enabled, AgentChannelConfig config, Instant createdAt) {
}
