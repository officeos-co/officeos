package com.enterpriseagentos.backendjava.domain.features.agents.interfaces;

import java.util.List;
import java.util.UUID;
import java.util.concurrent.CompletableFuture;

import com.enterpriseagentos.backendjava.domain.features.agents.enums.ToolPermission;
import com.enterpriseagentos.backendjava.domain.features.agents.models.AgentToolPermissionModel;

public interface IAgentToolPermissionRepository {
    CompletableFuture<List<AgentToolPermissionModel>> listForAgentAsync(UUID agentId);
    CompletableFuture<Void> upsertAsync(UUID agentId, String skillName, String toolName, ToolPermission permission);
    CompletableFuture<Void> setManyAsync(UUID agentId, List<AgentToolPermissionModel> entries);
}
