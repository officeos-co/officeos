package com.enterpriseagentos.backendjava.domain.common.services;

import java.util.List;

public final class ProviderDefinition  {
    private final String slug;
    private final String displayName;
    private final ApiFormat apiFormat;
    private final String baseUrl;
    private final String platformKeyConfigName;
    private final List<ModelDefinition> models;

    public ProviderDefinition(String slug, String displayName, ApiFormat apiFormat, String baseUrl, String platformKeyConfigName, List<ModelDefinition> models) {
        this.slug = slug;
        this.displayName = displayName;
        this.apiFormat = apiFormat;
        this.baseUrl = baseUrl;
        this.platformKeyConfigName = platformKeyConfigName;
        this.models = models;
    }

    public String getSlug() {
        return slug;
}

    public String slug() {
        return slug;
    }

    public String getDisplayName() {
        return displayName;
}

    public String displayName() {
        return displayName;
    }

    public ApiFormat getApiFormat() {
        return apiFormat;
}

    public ApiFormat apiFormat() {
        return apiFormat;
    }

    public String getBaseUrl() {
        return baseUrl;
}

    public String baseUrl() {
        return baseUrl;
    }

    public String getPlatformKeyConfigName() {
        return platformKeyConfigName;
}

    public String platformKeyConfigName() {
        return platformKeyConfigName;
    }

    public List<ModelDefinition> getModels() {
        return models;
}

    public List<ModelDefinition> models() {
        return models;
    }
}
