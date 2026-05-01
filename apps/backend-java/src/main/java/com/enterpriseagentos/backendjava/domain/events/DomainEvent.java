package com.enterpriseagentos.backendjava.domain.events;

import java.time.Instant;

public interface DomainEvent {
    default Instant occurredAt() {
        return Instant.now();
    }
}
