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

public interface IOrgBillingService {
    CompletableFuture<OrgSubscription> getSubscriptionAsync(String orgId);
    CompletableFuture<CreditBudgetResult> checkCreditBudgetAsync(String orgId);
    CompletableFuture<String> createCustomerAsync(String orgId, String email);
    CompletableFuture<String> createSubscriptionAsync(String customerId, String plan, String billingCycle);
    CompletableFuture<Void> enableOverageAsync(String orgId, String email, boolean enabled);
}
