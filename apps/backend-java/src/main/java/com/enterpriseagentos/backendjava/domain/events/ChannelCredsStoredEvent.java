package com.enterpriseagentos.backendjava.domain.events;

import java.util.UUID;

public final class ChannelCredsStoredEvent implements DomainEvent {
    private final UUID connectionId;

    public ChannelCredsStoredEvent(UUID connectionId) {
        this.connectionId = connectionId;
    }

    public UUID getConnectionId() {
        return connectionId;
}

    public UUID connectionId() {
        return connectionId;
    }
}
