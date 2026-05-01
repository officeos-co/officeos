package com.enterpriseagentos.backendjava.domain.common.valueobjects;

import java.util.Locale;

public enum AgentStatus {
    Pending,
    Running,
    Failed;

    public String toWire() {
        return switch (this) {
            case Pending -> "pending";
            case Running -> "running";
            case Failed -> "failed";
        };
    }

    public static AgentStatus fromWire(String value) {
        return switch (value.trim().toLowerCase(Locale.ROOT)) {
            case "pending" -> Pending;
            case "running" -> Running;
            case "failed" -> Failed;
            default -> throw new IllegalArgumentException("Unknown agent status: " + value);
        };
    }
}
