package com.enterpriseagentos.backendjava.domain.features.agents;

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

public class AgentRunRecord {
    public UUID id;
    public UUID agentId;
    public UUID parentRunId;
    public String parentCorrelationId;
    public String kind;
    public String status;
    public String name;
    public String description;
    public String prompt;
    public String result;
    public String error;
    public Instant createdAt;
    public Instant updatedAt;
    public Instant completedAt;
}
