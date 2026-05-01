package com.enterpriseagentos.backendjava.domain.features.analytics.dtos;

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

public final class TrackAgentCreatedInput  {
    private final String agentName;
    private final String provider;
    private final String template;
    private final int skillCount;
    private final int allowSkills;
    private final int denySkills;

    public TrackAgentCreatedInput(String agentName, String provider, String template, int skillCount, int allowSkills, int denySkills) {
        this.agentName = agentName;
        this.provider = provider;
        this.template = template;
        this.skillCount = skillCount;
        this.allowSkills = allowSkills;
        this.denySkills = denySkills;
    }

    public String getAgentName() {
        return agentName;
}

    public String agentName() {
        return agentName;
    }

    public String getProvider() {
        return provider;
}

    public String provider() {
        return provider;
    }

    public String getTemplate() {
        return template;
}

    public String template() {
        return template;
    }

    public int getSkillCount() {
        return skillCount;
}

    public int skillCount() {
        return skillCount;
    }

    public int getAllowSkills() {
        return allowSkills;
}

    public int allowSkills() {
        return allowSkills;
    }

    public int getDenySkills() {
        return denySkills;
}

    public int denySkills() {
        return denySkills;
    }
}
