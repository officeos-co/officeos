package com.enterpriseagentos.backendjava.domain.common.valueobjects;

import java.util.Locale;

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
