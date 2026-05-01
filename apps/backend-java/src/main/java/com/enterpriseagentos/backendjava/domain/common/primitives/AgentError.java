package com.enterpriseagentos.backendjava.domain.common.primitives;

import java.time.Instant;
import java.util.UUID;
import com.enterpriseagentos.backendjava.domain.features.analytics.AgentLogRecord;
import com.enterpriseagentos.backendjava.domain.features.analytics.AgentLogType;

public record AgentError(AgentErrorCategory category, String message, String detail) {
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

    public AgentLogRecord toLogRecord(UUID agentId) {
        AgentLogRecord record = new AgentLogRecord();
        record.agentId = agentId;
        record.type = logType();
        record.content = formattedContent();
        record.time = Instant.now();
        return record;
    }
}
