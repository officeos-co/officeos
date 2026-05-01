package com.enterpriseagentos.backendjava.api.graphql;

import com.enterpriseagentos.backendjava.api.rest.ApiException;
import com.enterpriseagentos.backendjava.application.agents.AgentApplicationService;
import com.enterpriseagentos.backendjava.application.agents.RecordToolInvocationCommand;
import com.enterpriseagentos.backendjava.domain.agents.Agent;
import com.enterpriseagentos.backendjava.domain.agents.ToolInvocation;
import com.enterpriseagentos.backendjava.domain.agents.ToolInvocationStatus;
import java.util.List;
import java.util.UUID;
import org.springframework.graphql.data.method.annotation.Argument;
import org.springframework.graphql.data.method.annotation.MutationMapping;
import org.springframework.graphql.data.method.annotation.QueryMapping;
import org.springframework.graphql.data.method.annotation.SchemaMapping;
import org.springframework.stereotype.Controller;

@Controller
public class AgentGraphqlController {
    private final AgentApplicationService agents;

    public AgentGraphqlController(AgentApplicationService agents) {
        this.agents = agents;
    }

    @QueryMapping
    public List<Agent> agents() {
        return agents.listAgents();
    }

    @MutationMapping
    public ToolInvocation recordToolInvocation(@Argument RecordToolInvocationInput input) {
        var command = new RecordToolInvocationCommand(
            input.agentId(),
            input.toolName(),
            input.status(),
            input.failureReason()
        );

        return agents.recordToolInvocation(command)
            .fold(
                invocation -> invocation,
                failure -> {
                    throw new ApiException(failure.code(), failure.message());
                }
            );
    }

    @SchemaMapping(typeName = "Agent", field = "toolInvocations")
    public List<ToolInvocation> toolInvocations(Agent agent) {
        return agents.listToolInvocations(agent.id());
    }

    public record RecordToolInvocationInput(
        UUID agentId,
        String toolName,
        ToolInvocationStatus status,
        String failureReason
    ) {
    }
}
