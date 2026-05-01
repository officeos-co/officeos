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

public enum SubscriptionPlan {
    Free,
    Pro,
    Team,
    Enterprise;

    public String toWire() {
        return switch (this) {
            case Free -> "free";
            case Pro -> "pro";
            case Team -> "team";
            case Enterprise -> "enterprise";
        };
    }

    public static SubscriptionPlan fromWire(String value) {
        if (value == null || value.isBlank()) throw new IllegalArgumentException("SubscriptionPlan is required.");
        return switch (value.trim().toLowerCase(Locale.ROOT)) {
            case "free" -> Free;
            case "pro" -> Pro;
            case "team" -> Team;
            case "enterprise" -> Enterprise;
            default -> throw new IllegalArgumentException("Unknown SubscriptionPlan: " + value);
        };
    }
}

