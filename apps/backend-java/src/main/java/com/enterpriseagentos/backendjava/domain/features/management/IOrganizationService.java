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

public interface IOrganizationService {
    CompletableFuture<OrgMemberRecord> inviteMemberAsync(UUID callerUserId, String callerEmail, String callerName, String memberEmail, String role);
    CompletableFuture<Boolean> removeMemberAsync(UUID callerUserId, String callerEmail, String callerName, UUID memberId);
    CompletableFuture<OrganizationRecord> renameAsync(UUID callerUserId, String callerEmail, String callerName, String name);
    CompletableFuture<List<OrgMemberRecord>> listMembersAsync(UUID callerUserId, String callerEmail, String callerName);
}
