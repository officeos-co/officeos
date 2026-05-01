package com.enterpriseagentos.backendjava.domain.features.management.dtos;

import com.enterpriseagentos.backendjava.domain.common.*;
import com.enterpriseagentos.backendjava.domain.common.models.*;
import com.enterpriseagentos.backendjava.domain.common.primitives.*;
import com.enterpriseagentos.backendjava.domain.common.services.*;
import com.enterpriseagentos.backendjava.domain.common.valueobjects.*;
import com.enterpriseagentos.backendjava.domain.events.*;
import com.enterpriseagentos.backendjava.domain.features.agents.dtos.*;
import com.enterpriseagentos.backendjava.domain.features.agents.enums.*;
import com.enterpriseagentos.backendjava.domain.features.agents.interfaces.*;
import com.enterpriseagentos.backendjava.domain.features.agents.models.*;
import com.enterpriseagentos.backendjava.domain.features.agents.registries.*;
import com.enterpriseagentos.backendjava.domain.features.analytics.dtos.*;
import com.enterpriseagentos.backendjava.domain.features.analytics.enums.*;
import com.enterpriseagentos.backendjava.domain.features.analytics.interfaces.*;
import com.enterpriseagentos.backendjava.domain.features.analytics.mappers.*;
import com.enterpriseagentos.backendjava.domain.features.analytics.models.*;
import com.enterpriseagentos.backendjava.domain.features.management.dtos.*;
import com.enterpriseagentos.backendjava.domain.features.management.exceptions.*;
import com.enterpriseagentos.backendjava.domain.features.management.interfaces.*;
import com.enterpriseagentos.backendjava.domain.features.management.models.*;
import com.enterpriseagentos.backendjava.domain.features.management.registries.*;
import com.enterpriseagentos.backendjava.domain.features.management.services.*;
import com.enterpriseagentos.backendjava.domain.features.mcp.dtos.*;
import com.enterpriseagentos.backendjava.domain.features.mcp.enums.*;
import com.enterpriseagentos.backendjava.domain.features.mcp.interfaces.*;
import com.enterpriseagentos.backendjava.domain.features.mcp.models.*;

import java.math.BigDecimal;
import java.time.Instant;
import java.time.YearMonth;
import java.util.*;
import java.util.concurrent.CompletableFuture;

public final class GdprExportDto  {
    private final GdprUserDto user;
    private final List<GdprAgentDto> agents;
    private final List<GdprConversationDto> conversations;
    private final List<GdprAuditEntryDto> auditEntries;

    public GdprExportDto(GdprUserDto user, List<GdprAgentDto> agents, List<GdprConversationDto> conversations, List<GdprAuditEntryDto> auditEntries) {
        this.user = user;
        this.agents = agents;
        this.conversations = conversations;
        this.auditEntries = auditEntries;
    }

    public GdprUserDto getUser() {
        return user;
}

    public GdprUserDto user() {
        return user;
    }

    public List<GdprAgentDto> getAgents() {
        return agents;
}

    public List<GdprAgentDto> agents() {
        return agents;
    }

    public List<GdprConversationDto> getConversations() {
        return conversations;
}

    public List<GdprConversationDto> conversations() {
        return conversations;
    }

    public List<GdprAuditEntryDto> getAuditEntries() {
        return auditEntries;
}

    public List<GdprAuditEntryDto> auditEntries() {
        return auditEntries;
    }
}
