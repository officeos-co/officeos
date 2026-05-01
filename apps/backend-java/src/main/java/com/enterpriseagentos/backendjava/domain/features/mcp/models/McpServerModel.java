package com.enterpriseagentos.backendjava.domain.features.mcp.models;

import com.enterpriseagentos.backendjava.domain.common.*;
import com.enterpriseagentos.backendjava.domain.common.models.*;
import com.enterpriseagentos.backendjava.domain.common.primitives.*;
import com.enterpriseagentos.backendjava.domain.common.services.*;
import com.enterpriseagentos.backendjava.domain.common.valueobjects.*;
import com.enterpriseagentos.backendjava.domain.events.*;
import com.enterpriseagentos.backendjava.domain.features.agents.dtos.*;
import com.enterpriseagentos.backendjava.domain.features.agents.enums.*;
import com.enterpriseagentos.backendjava.domain.features.agents.interfaces.*;
import com.enterpriseagentos.backendjava.domain.features.agents.models.*;
import com.enterpriseagentos.backendjava.domain.features.agents.registries.*;
import com.enterpriseagentos.backendjava.domain.features.analytics.dtos.*;
import com.enterpriseagentos.backendjava.domain.features.analytics.enums.*;
import com.enterpriseagentos.backendjava.domain.features.analytics.interfaces.*;
import com.enterpriseagentos.backendjava.domain.features.analytics.mappers.*;
import com.enterpriseagentos.backendjava.domain.features.analytics.models.*;
import com.enterpriseagentos.backendjava.domain.features.management.dtos.*;
import com.enterpriseagentos.backendjava.domain.features.management.exceptions.*;
import com.enterpriseagentos.backendjava.domain.features.management.interfaces.*;
import com.enterpriseagentos.backendjava.domain.features.management.models.*;
import com.enterpriseagentos.backendjava.domain.features.management.registries.*;
import com.enterpriseagentos.backendjava.domain.features.management.services.*;
import com.enterpriseagentos.backendjava.domain.features.mcp.dtos.*;
import com.enterpriseagentos.backendjava.domain.features.mcp.enums.*;
import com.enterpriseagentos.backendjava.domain.features.mcp.interfaces.*;
import com.enterpriseagentos.backendjava.domain.features.mcp.models.*;

import java.math.BigDecimal;
import java.time.Instant;
import java.time.YearMonth;
import java.util.*;
import java.util.concurrent.CompletableFuture;

public class McpServerModel {
    private String name;
    private String title;
    private String description;
    private McpTransportType transportType;
    private String command;
    private String args;
    private String url;
    private String logo;
    private String category;
    private String credentialFieldsJson;
    private String oauthProvider;
    private String oauthScopesJson;
    private boolean oauthConfigured;
    private String subtitle;
    private String authorName;
    private String authorUrl;
    private String documentationUrl;
    private String repositoryUrl;
    private String toolsJson;
    private boolean isBuiltin;
    private Instant createdAt;

    public String getName() {
        return name;
    }

    public String getTitle() {
        return title;
    }

    public String getDescription() {
        return description;
    }

    public McpTransportType getTransportType() {
        return transportType;
    }

    public String getCommand() {
        return command;
    }

    public String getArgs() {
        return args;
    }

    public String getUrl() {
        return url;
    }

    public String getLogo() {
        return logo;
    }

    public String getCategory() {
        return category;
    }

    public String getCredentialFieldsJson() {
        return credentialFieldsJson;
    }

    public String getOauthProvider() {
        return oauthProvider;
    }

    public String getOauthScopesJson() {
        return oauthScopesJson;
    }

    public boolean getOauthConfigured() {
        return oauthConfigured;
    }

    public String getSubtitle() {
        return subtitle;
    }

    public String getAuthorName() {
        return authorName;
    }

    public String getAuthorUrl() {
        return authorUrl;
    }

    public String getDocumentationUrl() {
        return documentationUrl;
    }

    public String getRepositoryUrl() {
        return repositoryUrl;
    }

    public String getToolsJson() {
        return toolsJson;
    }

    public boolean getIsBuiltin() {
        return isBuiltin;
    }

    public Instant getCreatedAt() {
        return createdAt;
    }
}
