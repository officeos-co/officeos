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

public enum OrgRole {
    Owner,
    Admin,
    Member;

    public String toWire() {
        return switch (this) {
            case Owner -> "owner";
            case Admin -> "admin";
            case Member -> "member";
        };
    }

    public static OrgRole fromWire(String value) {
        if (value == null || value.isBlank()) throw new IllegalArgumentException("OrgRole is required.");
        return switch (value.trim().toLowerCase(Locale.ROOT)) {
            case "owner" -> Owner;
            case "admin" -> Admin;
            case "member" -> Member;
            default -> throw new IllegalArgumentException("Unknown OrgRole: " + value);
        };
    }
}

