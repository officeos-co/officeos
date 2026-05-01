package com.enterpriseagentos.backendjava.domain.features.agents.dtos;

import java.util.List;

public final class ChannelTypeDto  {
    private final String type;
    private final String displayName;
    private final String description;
    private final List<OnboardingStep> onboardingSteps;

    public ChannelTypeDto(String type, String displayName, String description, List<OnboardingStep> onboardingSteps) {
        this.type = type;
        this.displayName = displayName;
        this.description = description;
        this.onboardingSteps = onboardingSteps;
    }

    public String getType() {
        return type;
}

    public String type() {
        return type;
    }

    public String getDisplayName() {
        return displayName;
}

    public String displayName() {
        return displayName;
    }

    public String getDescription() {
        return description;
}

    public String description() {
        return description;
    }

    public List<OnboardingStep> getOnboardingSteps() {
        return onboardingSteps;
}

    public List<OnboardingStep> onboardingSteps() {
        return onboardingSteps;
    }
}
