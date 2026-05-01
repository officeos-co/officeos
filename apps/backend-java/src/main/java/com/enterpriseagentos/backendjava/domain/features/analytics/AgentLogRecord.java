package com.enterpriseagentos.backendjava.domain.features.analytics;

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

public class AgentLogRecord {
    public UUID id;
    public UUID agentId;
    public AgentRecord agent;
    public Instant time;
    public AgentLogType type;
    public String tool;
    public String integration;
    public String channel;
    public String content;
    public TokenUsage usage;
    public String correlationId;
    public UUID runId;
    public UUID parentRunId;
}
