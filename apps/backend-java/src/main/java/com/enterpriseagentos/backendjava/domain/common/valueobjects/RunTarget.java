package com.enterpriseagentos.backendjava.domain.common.valueobjects;

import java.util.Locale;

public enum RunTarget {
    Cloud,
    Runner;

    public String toWire() {
        return switch (this) {
            case Cloud -> "cloud";
            case Runner -> "runner";
        };
    }

    public static RunTarget fromWire(String value) {
        return switch (value.trim().toLowerCase(Locale.ROOT)) {
            case "cloud" -> Cloud;
            case "runner" -> Runner;
            default -> throw new IllegalArgumentException("Unknown run target: " + value);
        };
    }
}
