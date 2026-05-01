package com.enterpriseagentos.backendjava.domain.features.mcp.dtos;

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

public class McpDiscoveredTool {
    private String name;
    private String description;
    private String jsonSchema;
    private String serverName;
    private Object nativeHandle;

    public String getName() {
        return name;
    }

    public String getDescription() {
        return description;
    }

    public String getJsonSchema() {
        return jsonSchema;
    }

    public String getServerName() {
        return serverName;
    }

    public Object getNativeHandle() {
        return nativeHandle;
    }
}
