package com.enterpriseagentos.backendjava.domain.common.primitives;

import java.time.Instant;
import java.util.UUID;

import com.enterpriseagentos.backendjava.domain.features.analytics.enums.AgentLogType;
import com.enterpriseagentos.backendjava.domain.features.analytics.models.AgentLogModel;

public final class AgentError  {
    private final AgentErrorCategory category;
    private final String message;
    private final String detail;

    public AgentError(AgentErrorCategory category, String message, String detail) {
        this.category = category;
        this.message = message;
        this.detail = detail;
    }

    public AgentErrorCategory getCategory() {
        return category;
}

    public AgentErrorCategory category() {
        return category;
    }

    public String getMessage() {
        return message;
}

    public String message() {
        return message;
    }

    public String getDetail() {
        return detail;
}

    public String detail() {
        return detail;
    }

    public AgentLogType logType() {
        return switch (category) {
            case PodConnection -> AgentLogType.ErrorPodConnection;
            case LlmCall -> AgentLogType.ErrorLlmCall;
            case ToolExecution -> AgentLogType.ErrorToolExecution;
            case SkillExecution -> AgentLogType.ErrorSkillExecution;
            case TurnOrchestration -> AgentLogType.ErrorTurnOrchestration;
            case Memory -> AgentLogType.ErrorMemory;
            case Configuration -> AgentLogType.ErrorConfiguration;
        };
    }

    public String formattedContent() {
        String prefix = category + ": " + message;
        return detail == null ? prefix : prefix + "\n" + detail;
    }

    public AgentLogModel toLogModel(UUID agentId) {
        return AgentLogModel.error(agentId, logType(), formattedContent(), Instant.now());
    }
}
