package com.enterpriseagentos.backendjava.domain.common.valueobjects;

import java.time.Instant;

public final class BillingPeriod  {
    private final Instant start;
    private final Instant end;

    public BillingPeriod(Instant start, Instant end) {
        this.start = start;
        this.end = end;
    }

    public Instant getStart() {
        return start;
}

    public Instant start() {
        return start;
    }

    public Instant getEnd() {
        return end;
}

    public Instant end() {
        return end;
    }
}
