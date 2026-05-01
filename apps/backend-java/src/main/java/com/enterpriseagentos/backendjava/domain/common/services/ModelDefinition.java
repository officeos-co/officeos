package com.enterpriseagentos.backendjava.domain.common.services;

public final class ModelDefinition  {
    private final String id;
    private final String displayName;
    private final int costWeight;
    private final SmartRoutingTier smartTier;

    public ModelDefinition(String id, String displayName, int costWeight, SmartRoutingTier smartTier) {
        this.id = id;
        this.displayName = displayName;
        this.costWeight = costWeight;
        this.smartTier = smartTier;
    }

    public String getId() {
        return id;
}

    public String id() {
        return id;
    }

    public String getDisplayName() {
        return displayName;
}

    public String displayName() {
        return displayName;
    }

    public int getCostWeight() {
        return costWeight;
}

    public int costWeight() {
        return costWeight;
    }

    public SmartRoutingTier getSmartTier() {
        return smartTier;
}

    public SmartRoutingTier smartTier() {
        return smartTier;
    }
}
