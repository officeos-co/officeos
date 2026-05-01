package com.enterpriseagentos.backendjava.application.agents;

import com.enterpriseagentos.backendjava.domain.agents.Agent;
import com.enterpriseagentos.backendjava.domain.agents.AgentNamePolicy;
import com.enterpriseagentos.backendjava.domain.agents.AgentRegisteredEvent;
import com.enterpriseagentos.backendjava.domain.agents.AgentRepository;
import com.enterpriseagentos.backendjava.domain.agents.AgentStatus;
import com.enterpriseagentos.backendjava.domain.agents.ToolInvocation;
import com.enterpriseagentos.backendjava.domain.agents.ToolInvocationRecordedEvent;
import com.enterpriseagentos.backendjava.domain.agents.ToolInvocationRepository;
import com.enterpriseagentos.backendjava.domain.agents.ToolInvocationStatus;
import com.enterpriseagentos.backendjava.domain.common.Result;
import java.time.Clock;
import java.time.OffsetDateTime;
import java.util.List;
import java.util.Optional;
import java.util.UUID;
import org.springframework.context.ApplicationEventPublisher;
import org.springframework.stereotype.Service;
import org.springframework.transaction.annotation.Transactional;

@Service
public class AgentApplicationService {
    private final AgentRepository agentRepository;
    private final ToolInvocationRepository toolInvocationRepository;
    private final AgentNamePolicy agentNamePolicy;
    private final ApplicationEventPublisher events;
    private final Clock clock;

    public AgentApplicationService(
        AgentRepository agentRepository,
        ToolInvocationRepository toolInvocationRepository,
        AgentNamePolicy agentNamePolicy,
        ApplicationEventPublisher events,
        Clock clock
    ) {
        this.agentRepository = agentRepository;
        this.toolInvocationRepository = toolInvocationRepository;
        this.agentNamePolicy = agentNamePolicy;
        this.events = events;
        this.clock = clock;
    }

    @Transactional(readOnly = true)
    public List<Agent> listAgents() {
        return agentRepository.findAll();
    }

    @Transactional(readOnly = true)
    public Optional<Agent> getAgent(UUID id) {
        return agentRepository.findById(id);
    }

    @Transactional(readOnly = true)
    public List<ToolInvocation> listToolInvocations(UUID agentId) {
        return toolInvocationRepository.findByAgentId(agentId);
    }

    @Transactional
    public Result<Agent> registerAgent(String name) {
        if (!agentNamePolicy.isValid(name)) {
            return Result.failure("AGENT_NAME_INVALID", "Agent name is required and must be 120 characters or fewer.");
        }

        var agent = new Agent(
            UUID.randomUUID(),
            agentNamePolicy.normalize(name),
            AgentStatus.ACTIVE,
            OffsetDateTime.now(clock)
        );
        var saved = agentRepository.save(agent);
        events.publishEvent(new AgentRegisteredEvent(saved.id()));
        return Result.success(saved);
    }

    @Transactional
    public Result<ToolInvocation> recordToolInvocation(RecordToolInvocationCommand command) {
        if (agentRepository.findById(command.agentId()).isEmpty()) {
            return Result.failure("AGENT_NOT_FOUND", "Agent does not exist.");
        }
        if (command.toolName() == null || command.toolName().isBlank() || command.toolName().length() > 120) {
            return Result.failure("TOOL_NAME_INVALID", "Tool name is required and must be 120 characters or fewer.");
        }
        if (command.status() == ToolInvocationStatus.FAILED
            && (command.failureReason() == null || command.failureReason().isBlank())) {
            return Result.failure("FAILURE_REASON_REQUIRED", "Failed tool invocations must include a failure reason.");
        }

        var invocation = new ToolInvocation(
            UUID.randomUUID(),
            command.agentId(),
            command.toolName().trim(),
            command.status(),
            command.failureReason(),
            OffsetDateTime.now(clock)
        );
        var saved = toolInvocationRepository.save(invocation);
        events.publishEvent(new ToolInvocationRecordedEvent(saved.id(), saved.agentId(), saved.status()));
        return Result.success(saved);
    }
}
