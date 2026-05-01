package com.enterpriseagentos.backendjava.domain.common.valueobjects;

import java.math.BigDecimal;
import java.time.Instant;
import java.time.YearMonth;
import java.util.*;
import java.util.concurrent.CompletableFuture;
import com.enterpriseagentos.backendjava.domain.common.primitives.*;
import com.enterpriseagentos.backendjava.domain.common.services.*;
import com.enterpriseagentos.backendjava.domain.common.valueobjects.*;
import com.enterpriseagentos.backendjava.domain.events.*;
import com.enterpriseagentos.backendjava.domain.features.agents.*;
import com.enterpriseagentos.backendjava.domain.features.analytics.*;
import com.enterpriseagentos.backendjava.domain.features.management.*;
import com.enterpriseagentos.backendjava.domain.features.mcp.*;

public enum BillingCycle {
    Monthly,
    Yearly;

    public String toWire() {
        return switch (this) {
            case Monthly -> "monthly";
            case Yearly -> "yearly";
        };
    }

    public static BillingCycle fromWire(String value) {
        if (value == null || value.isBlank()) throw new IllegalArgumentException("BillingCycle is required.");
        return switch (value.trim().toLowerCase(Locale.ROOT)) {
            case "monthly" -> Monthly;
            case "yearly" -> Yearly;
            default -> throw new IllegalArgumentException("Unknown BillingCycle: " + value);
        };
    }
}

