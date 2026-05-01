package com.enterpriseagentos.backendjava.domain.features.agents.dtos;

import com.enterpriseagentos.backendjava.domain.features.agents.enums.ToolPermission;

public final class AgentToolPermissionInit  {
    private final String tool;
    private final ToolPermission mode;

    public AgentToolPermissionInit(String tool, ToolPermission mode) {
        this.tool = tool;
        this.mode = mode;
    }

    public String getTool() {
        return tool;
}

    public String tool() {
        return tool;
    }

    public ToolPermission getMode() {
        return mode;
}

    public ToolPermission mode() {
        return mode;
    }
}
