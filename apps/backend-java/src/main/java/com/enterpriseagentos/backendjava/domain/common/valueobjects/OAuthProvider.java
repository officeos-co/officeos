package com.enterpriseagentos.backendjava.domain.common.valueobjects;

import java.util.Locale;

public enum OAuthProvider {
    Google,
    Microsoft,
    GitHub;

    public String toWire() {
        return switch (this) {
            case Google -> "google";
            case Microsoft -> "microsoft";
            case GitHub -> "github";
        };
    }

    public static OAuthProvider fromWire(String value) {
        return switch (value.trim().toLowerCase(Locale.ROOT)) {
            case "google" -> Google;
            case "microsoft" -> Microsoft;
            case "github" -> GitHub;
            default -> throw new IllegalArgumentException("Unknown OAuth provider: " + value);
        };
    }
}
