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

public enum AgentLogType {
    ToolCall,
    ToolResult,
    MessageIn,
    MessageOut,
    ChannelIn,
    ChannelOut,
    System,
    AgentStartup,
    AgentShutdown,
    Error,
    ErrorPodConnection,
    ErrorLlmCall,
    ErrorToolExecution,
    ErrorSkillExecution,
    ErrorTurnOrchestration,
    ErrorMemory,
    ErrorConfiguration;}

