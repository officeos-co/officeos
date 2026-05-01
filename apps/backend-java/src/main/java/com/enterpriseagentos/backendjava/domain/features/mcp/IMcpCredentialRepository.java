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

public interface IMcpCredentialRepository {
    CompletableFuture<McpCredentialRecord> getByServerNameAsync(String serverName);
    CompletableFuture<Void> upsertAsync(McpCredentialRecord credential);
    CompletableFuture<Void> deleteAsync(String serverName);
}
