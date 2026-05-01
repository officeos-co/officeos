package com.enterpriseagentos.backendjava.domain.features.agents.dtos;

import java.util.List;

public final class ChannelTypeDefinition  {
    private final String type;
    private final String displayName;
    private final String description;
    private final String logo;
    private final List<OnboardingStep> onboardingSteps;

    public ChannelTypeDefinition(String type, String displayName, String description, String logo, List<OnboardingStep> onboardingSteps) {
        this.type = type;
        this.displayName = displayName;
        this.description = description;
        this.logo = logo;
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

    public String getLogo() {
        return logo;
}

    public String logo() {
        return logo;
    }

    public List<OnboardingStep> getOnboardingSteps() {
        return onboardingSteps;
}

    public List<OnboardingStep> onboardingSteps() {
        return onboardingSteps;
    }
}
