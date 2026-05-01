package com.enterpriseagentos.backendjava.domain.features.agents.dtos;

public final class OnboardingStep  {
    private final String type;
    private final String title;
    private final String description;
    private final String value;
    private final String inputKey;
    private final String inputLabel;
    private final String inputPlaceholder;
    private final String inputHelp;
    private final String inputKind;
    private final boolean inputRequired;

    public OnboardingStep(String type, String title, String description, String value, String inputKey, String inputLabel, String inputPlaceholder, String inputHelp, String inputKind, boolean inputRequired) {
        this.type = type;
        this.title = title;
        this.description = description;
        this.value = value;
        this.inputKey = inputKey;
        this.inputLabel = inputLabel;
        this.inputPlaceholder = inputPlaceholder;
        this.inputHelp = inputHelp;
        this.inputKind = inputKind;
        this.inputRequired = inputRequired;
    }

    public String getType() {
        return type;
}

    public String type() {
        return type;
    }

    public String getTitle() {
        return title;
}

    public String title() {
        return title;
    }

    public String getDescription() {
        return description;
}

    public String description() {
        return description;
    }

    public String getValue() {
        return value;
}

    public String value() {
        return value;
    }

    public String getInputKey() {
        return inputKey;
}

    public String inputKey() {
        return inputKey;
    }

    public String getInputLabel() {
        return inputLabel;
}

    public String inputLabel() {
        return inputLabel;
    }

    public String getInputPlaceholder() {
        return inputPlaceholder;
}

    public String inputPlaceholder() {
        return inputPlaceholder;
    }

    public String getInputHelp() {
        return inputHelp;
}

    public String inputHelp() {
        return inputHelp;
    }

    public String getInputKind() {
        return inputKind;
}

    public String inputKind() {
        return inputKind;
    }

    public boolean getInputRequired() {
        return inputRequired;
}

    public boolean inputRequired() {
        return inputRequired;
    }
}
