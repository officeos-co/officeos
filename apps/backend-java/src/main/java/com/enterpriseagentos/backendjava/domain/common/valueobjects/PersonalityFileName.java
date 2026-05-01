package com.enterpriseagentos.backendjava.domain.common.valueobjects;

import java.util.List;

public final class PersonalityFileName implements Comparable<PersonalityFileName> {
    private final String value;

    public PersonalityFileName(String value) {
        if (value == null || value.isBlank()) {
        throw new IllegalArgumentException("Personality file name must not be empty.");
        }
        if (value.length() > 128) {
        throw new IllegalArgumentException("Personality file name must not exceed 128 characters.");
        }
        value = value.trim();

        this.value = value;
    }

    public String getValue() {
        return value;
}

    public String value() {
        return value;
    }

    public static final List<String> KNOWN_FILE_NAMES = List.of(
        "AGENTS.md",
        "SOUL.md",
        "TOOLS.md",
        "IDENTITY.md",
        "USER.md",
        "BOOTSTRAP.md"
    );

    @Override
    public int compareTo(PersonalityFileName other) {
        return Integer.compare(order(value), order(other.value));
    }

    private static int order(String fileName) {
        for (int index = 0; index < KNOWN_FILE_NAMES.size(); index++) {
            if (KNOWN_FILE_NAMES.get(index).equalsIgnoreCase(fileName)) {
                return index;
            }
        }
        return KNOWN_FILE_NAMES.size() + 1;
    }
}
