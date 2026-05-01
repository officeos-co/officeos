package com.enterpriseagentos.backendjava.domain.features.mcp;

import java.math.BigDecimal;
import java.time.Instant;
import java.time.YearMonth;
import java.util.*;
import java.util.concurrent.CompletableFuture;
import com.enterpriseagentos.backendjava.domain.common.primitives.*;
import com.enterpriseagentos.backendjava.domain.common.services.*;
import com.enterpriseagentos.backendjava.domain.common.valueobjects.*;
import com.enterpriseagentos.backendjava.domain.events.*;
import com.enterpriseagentos.backendjava.domain.features.agents.*;
import com.enterpriseagentos.backendjava.domain.features.analytics.*;
import com.enterpriseagentos.backendjava.domain.features.management.*;
import com.enterpriseagentos.backendjava.domain.features.mcp.*;

public class McpServerRecord {
    public String name;
    public String title;
    public String description;
    public McpTransportType transportType;
    public String command;
    public String args;
    public String url;
    public String logo;
    public String category;
    public String credentialFieldsJson;
    public String oauthProvider;
    public String oauthScopesJson;
    public boolean oauthConfigured;
    public String subtitle;
    public String authorName;
    public String authorUrl;
    public String documentationUrl;
    public String repositoryUrl;
    public String toolsJson;
    public boolean isBuiltin;
    public Instant createdAt;
}
