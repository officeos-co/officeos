package com.enterpriseagentos.backendjava.domain.common.valueobjects;

import java.util.Locale;

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
