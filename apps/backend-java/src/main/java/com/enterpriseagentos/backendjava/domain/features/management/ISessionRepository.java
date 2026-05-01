package com.enterpriseagentos.backendjava.domain.features.management;

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

public interface ISessionRepository {
    CompletableFuture<SessionRecord> createAsync(UUID userId, String tokenHash, Instant expiresAt);
    CompletableFuture<SessionRecord> getByTokenHashAsync(String tokenHash);
    CompletableFuture<Void> deleteAsync(String tokenHash);
    CompletableFuture<Void> purgeExpiredAsync();
    CompletableFuture<Void> deleteByUserIdAsync(UUID userId);
}
