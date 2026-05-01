package com.enterpriseagentos.backendjava.domain.common.valueobjects;

import java.util.Locale;

public enum SessionStatus {
    Active,
    Ended;

    public String toWire() {
        return switch (this) {
            case Active -> "active";
            case Ended -> "ended";
        };
    }

    public static SessionStatus fromWire(String value) {
        return switch (value.trim().toLowerCase(Locale.ROOT)) {
            case "active" -> Active;
            case "ended" -> Ended;
            default -> throw new IllegalArgumentException("Unknown session status: " + value);
        };
    }
}
