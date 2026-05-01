package com.enterpriseagentos.backendjava.domain.events;

import java.util.UUID;

public final class LlmCallCompletedEvent implements DomainEvent {
    private final UUID agentId;
    private final String correlationId;
    private final String model;
    private final int durationMs;
    private final int inputTokens;
    private final int outputTokens;

    public LlmCallCompletedEvent(UUID agentId, String correlationId, String model, int durationMs, int inputTokens, int outputTokens) {
        this.agentId = agentId;
        this.correlationId = correlationId;
        this.model = model;
        this.durationMs = durationMs;
        this.inputTokens = inputTokens;
        this.outputTokens = outputTokens;
    }

    public UUID getAgentId() {
        return agentId;
}

    public UUID agentId() {
        return agentId;
    }

    public String getCorrelationId() {
        return correlationId;
}

    public String correlationId() {
        return correlationId;
    }

    public String getModel() {
        return model;
}

    public String model() {
        return model;
    }

    public int getDurationMs() {
        return durationMs;
}

    public int durationMs() {
        return durationMs;
    }

    public int getInputTokens() {
        return inputTokens;
}

    public int inputTokens() {
        return inputTokens;
    }

    public int getOutputTokens() {
        return outputTokens;
}

    public int outputTokens() {
        return outputTokens;
    }
}
