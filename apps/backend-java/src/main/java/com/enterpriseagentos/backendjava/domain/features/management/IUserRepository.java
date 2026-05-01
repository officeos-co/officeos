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

public interface IUserRepository {
    CompletableFuture<UserRecord> upsertByGoogleSubjectAsync(String googleSubjectId, String email, String name, String avatarUrl);
    CompletableFuture<UserRecord> upsertByGitHubSubjectAsync(String gitHubSubjectId, String email, String name, String avatarUrl);
    CompletableFuture<UserRecord> getByIdAsync(UUID id);
    CompletableFuture<Void> deleteAsync(UUID id);
}
