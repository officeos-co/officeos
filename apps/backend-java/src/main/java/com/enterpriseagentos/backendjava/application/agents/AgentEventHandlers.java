package com.enterpriseagentos.backendjava.application.agents;

import com.enterpriseagentos.backendjava.domain.agents.AgentRegisteredEvent;
import com.enterpriseagentos.backendjava.domain.agents.ToolInvocationRecordedEvent;
import org.slf4j.Logger;
import org.slf4j.LoggerFactory;
import org.springframework.context.event.EventListener;
import org.springframework.stereotype.Component;

@Component
public class AgentEventHandlers {
    private static final Logger LOGGER = LoggerFactory.getLogger(AgentEventHandlers.class);

    @EventListener
    public void onAgentRegistered(AgentRegisteredEvent event) {
        LOGGER.info("agent_registered agent_id={}", event.agentId());
    }

    @EventListener
    public void onToolInvocationRecorded(ToolInvocationRecordedEvent event) {
        LOGGER.info(
            "tool_invocation_recorded tool_invocation_id={} agent_id={} status={}",
            event.toolInvocationId(),
            event.agentId(),
            event.status()
        );
    }
}
