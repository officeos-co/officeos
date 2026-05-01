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

public interface IUserBillingService {
    CompletableFuture<UserSubscription> getSubscriptionAsync(UUID userId);
    CompletableFuture<CreditBudgetResult> checkCreditBudgetAsync(UUID userId);
    CompletableFuture<String> createCheckoutSessionAsync(UUID userId, String email, String plan, String billingCycle);
    CompletableFuture<String> createPortalSessionAsync(UUID userId, String email);
    CompletableFuture<Void> enableOverageAsync(UUID userId, String email, boolean enabled);
}
