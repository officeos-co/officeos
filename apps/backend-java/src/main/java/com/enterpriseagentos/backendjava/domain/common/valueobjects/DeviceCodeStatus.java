package com.enterpriseagentos.backendjava.domain.common.valueobjects;

import java.util.Locale;

public enum DeviceCodeStatus {
    Pending,
    Authorized,
    Expired;

    public String toWire() {
        return switch (this) {
            case Pending -> "pending";
            case Authorized -> "authorized";
            case Expired -> "expired";
        };
    }

    public static DeviceCodeStatus fromWire(String value) {
        return switch (value.trim().toLowerCase(Locale.ROOT)) {
            case "pending" -> Pending;
            case "authorized" -> Authorized;
            case "expired" -> Expired;
            default -> throw new IllegalArgumentException("Unknown device code status: " + value);
        };
    }
}
