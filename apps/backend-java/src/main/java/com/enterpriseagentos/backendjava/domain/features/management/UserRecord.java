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

public class UserRecord {
    public UUID id;
    public String email;
    public String name;
    public String avatarUrl;
    public String googleSubjectId;
    public String gitHubSubjectId;
    public Instant createdAt;
    public Instant lastLoginAt;
    public String displayName;
    public String timezone;
    public String notificationPrefsJson;
    public String preferences;
    public UserSubscription subscription;
}
