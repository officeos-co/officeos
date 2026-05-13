using OffceOs.Application.Features.Agents;
using OffceOs.Database;
using OffceOs.Domain.Events;
using OffceOs.Domain.Common.ValueObjects;
using OffceOs.Domain.Features.AgentRoutines;
using OffceOs.Domain.Features.Agents;
using OffceOs.Domain.Features.Channels;
using OffceOs.Domain.Features.Context;
using OffceOs.Infrastructure.Features.Agents;
using OffceOs.Infrastructure.Features.AgentRoutines;
using OffceOs.Infrastructure.Features.Channels;
using OffceOs.Infrastructure.Features.Context;
using OffceOs.Infrastructure.Features.Integrations;
using OffceOs.Tests.Shared;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace OffceOs.Tests.Quickstart;

public sealed class QuickstartAgentEndToEndTests
{
    [Fact]
    public async Task Yaml_definition_creates_agent_and_exposes_configured_toolsets()
    {
        await using var db = WorkspaceTestHarness.CreateDb("quickstart-agent-e2e");
        var ownerId = Guid.NewGuid();
        await WorkspaceTestHarness.SeedUserAsync(db, ownerId, "owner@example.com");
        var harness = WorkspaceTestHarness.Create(db);
        var workspace = await harness.Workspaces.GetCurrentAsync(ownerId);
        await harness.Integrations.SaveCredentialAsync(
            ownerId,
            workspace.Id,
            "asana",
            new Dictionary<string, string> { ["ASANA_ACCESS_TOKEN"] = "test-token" });
        var browser = await new AgentResourceRepository(db).CreateBrowserResourceAsync(
            BrowserResourceRecord.Create(ownerId, workspace.Id, "Ops Browser"));
        var memoryStore = await new MemoryStoreRepository(db).CreateAsync(
            MemoryStoreRecord.Create(ownerId, workspace.Id, "Contract Memory"));
        var channel = await new ChannelRepository(db).CreateConnectionAsync(
            ChannelConnectionRecord.Create(ChannelType.Slack, "Ops Slack", ownerId, workspace.Id));

        var agent = await harness.AgentDashboard.CreateAsync(
            new CreateDashboardAgentRequest(
                Name: "Ignored name",
                Provider: "openai",
                Model: "gpt-4o-mini",
                Prompt: null,
                ConfigJson: $"""
                name: Asana planner
                description: Creates and reviews Asana task plans.
                model: gpt-4o-mini
                system: |-
                  Create Asana tasks from planning requests.
                mcp_servers:
                  - name: asana
                    type: registered
                tools:
                  - type: agent_toolset_20260401
                    default_config:
                      permission_policy:
                        type: allow_list
                        tools:
                          - file_read
                  - type: mcp_toolset
                    mcp_server_name: asana
                    default_config:
                      permission_policy:
                        type: allow_list
                        tools:
                          - create_task
                          - list_tasks
                  - type: browser_toolset
                    default_config:
                      permission_policy:
                        type: always_allow
                resources:
                  - type: browser
                    resource_id: {browser.Id}
                    access_mode: read_write
                    instructions: Use this browser for UI verification.
                  - type: memory_store
                    resource_id: {memoryStore.Id}
                    access_mode: read_only
                    instructions: Read contract notes before creating tasks.
                  - type: channel
                    resource_id: {channel.Id}
                    access_mode: read_write
                    instructions: Send concise updates to the team.
                routines:
                  - name: Daily planning sweep
                    prompt: Review open planning tasks and summarize blockers.
                    schedule_triggers:
                      - name: Weekday morning
                        expression: "0 9 * * 1-5"
                metadata:
                  template: quickstart-test
                """,
                IntegrationSlugs: null,
                ChannelConnectionIds: null,
                ToolNames: null,
                Resources: null,
                BootstrapMessage: null),
            ownerId,
            workspace.Id);

        var assignedIntegrations = await harness.Integrations.ListForAgentAsync(agent.Id, ownerId);
        var session = await new AgentSessionRepository(db).GetByAsync(new AgentSessionFilter
        {
            AgentId = agent.Id,
            Status = SessionStatus.Active,
        });
        var attachments = await new AgentResourceRepository(db).ListSessionAttachmentsAsync(session!.Id);
        var bindings = await new ChannelRepository(db).ListBindingsAsync(agent.Id);
        var routines = await new AgentRoutineRepository(db).ListAsync(new AgentRoutineFilter { AgentId = agent.Id });
        var registry = await CreateRegistryFactory(db).CreateAsync(new ToolRegistryRequest
        {
            Sandbox = new FakeAgentSandbox(),
            SandboxId = "sandbox",
            ServiceUrl = "http://sandbox",
            AgentId = agent.Id,
            WorkspaceId = workspace.Id,
            OwnerId = ownerId,
            CorrelationId = "quickstart-test",
            Integrations = assignedIntegrations,
        }, CancellationToken.None);
        await using (registry)
        {
            var toolNames = registry.Tools.Select(tool => tool.Name).ToHashSet(StringComparer.Ordinal);

            Assert.Equal("Asana planner", agent.Name);
            Assert.Contains(assignedIntegrations, integration => integration.Name == "asana");
            Assert.Contains(attachments, attachment => attachment.ResourceType == AgentResourceKinds.Browser && attachment.ResourceId == browser.Id);
            Assert.Contains(attachments, attachment => attachment.ResourceType == AgentResourceKinds.MemoryStore && attachment.ResourceId == memoryStore.Id);
            Assert.Contains(attachments, attachment => attachment.ResourceType == AgentResourceKinds.Channel && attachment.ResourceId == channel.Id);
            Assert.Contains(bindings, binding => binding.ChannelConnectionId == channel.Id);
            var routine = Assert.Single(routines);
            Assert.Equal("Daily planning sweep", routine.Name);
            Assert.Single(routine.Triggers);
            Assert.Contains("file_read", toolNames);
            Assert.DoesNotContain("shell", toolNames);
            Assert.Contains("asana__create_task", toolNames);
            Assert.Contains("asana__list_tasks", toolNames);
            Assert.DoesNotContain("asana__update_task", toolNames);
        }
    }

    private static ToolRegistryFactory CreateRegistryFactory(EaosDbContext db) =>
        new(
            new FakeAgentMemoryService(),
            new FakeAgentRoutineRepository(),
            new FakeAgentRoutineService(),
            new FakeAgentRunRepository(),
            new AgentTaskStore(),
            new BrowserToolService(new NoBrowserToolContextFactory()),
            new AgentDefinitionRepository(db),
            new AgentDefinitionParser(),
            new FakeOrganizationPolicyService(),
            new FakeIntegrationRuntimeService(),
            new TurnEventPublisher(new NoopPublisher()),
            NullLogger<ToolRegistryFactory>.Instance);
}
