package com.enterpriseagentos.backendjava.domain.common.valueobjects;

import java.util.Locale;

public enum MemberStatus {
    Active,
    Invited;

    public String toWire() {
        return switch (this) {
            case Active -> "active";
            case Invited -> "invited";
        };
    }

    public static MemberStatus fromWire(String value) {
        return switch (value.trim().toLowerCase(Locale.ROOT)) {
            case "active" -> Active;
            case "invited" -> Invited;
            default -> throw new IllegalArgumentException("Unknown member status: " + value);
        };
    }
}
