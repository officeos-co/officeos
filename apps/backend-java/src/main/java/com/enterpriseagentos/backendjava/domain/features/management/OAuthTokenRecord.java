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

public class OAuthTokenRecord {
    public UUID id;
    public String provider;
    public String encryptedAccessToken;
    public String encryptedRefreshToken;
    public List<OAuthGrantedScopeRecord> grantedScopes;
    public Instant expiresAtUtc;
    public String email;
    public Instant createdAt;
    public Instant updatedAt;
}
