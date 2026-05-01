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

public interface IMcpServerService {
    CompletableFuture<List<McpServerRecord>> listAsync();
    CompletableFuture<McpServerRecord> getAsync(String name);
    CompletableFuture<McpServerRecord> registerAsync(McpServerRecord server);
    CompletableFuture<Void> deleteAsync(String name);
    CompletableFuture<List<McpServerRecord>> listForAgentAsync(UUID agentId);
    CompletableFuture<Void> assignToAgentAsync(UUID agentId, String serverName);
    CompletableFuture<Void> unassignFromAgentAsync(UUID agentId, String serverName);
    CompletableFuture<Void> saveCredentialAsync(String serverName, Map<String, String> fields);
    CompletableFuture<Map<String, String>> getDecryptedCredentialAsync(String serverName);
}
