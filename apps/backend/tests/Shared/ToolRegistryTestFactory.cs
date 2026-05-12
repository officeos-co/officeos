using OffceOs.Application.Features.Agents;
using OffceOs.Domain.Events;
using OffceOs.Domain.Features.Management;
using Microsoft.Extensions.Logging.Abstractions;

namespace OffceOs.Tests.Shared;

internal static class ToolRegistryTestFactory
{
    public static ToolRegistryFactory CreateFactory(OrganizationPolicyProfileRecord? policy) =>
        new(
            new FakeAgentMemoryService(),
            new FakeAgentRoutineRepository(),
            new FakeAgentRunRepository(),
            new AgentTaskStore(),
            new BrowserToolService(new NoBrowserToolContextFactory()),
            new EmptyAgentDefinitionRepository(),
            new AgentDefinitionParser(),
            new FakeOrganizationPolicyService(policy),
            new FakeIntegrationRuntimeService(),
            new TurnEventPublisher(new NoopPublisher()),
            NullLogger<ToolRegistryFactory>.Instance);
}
