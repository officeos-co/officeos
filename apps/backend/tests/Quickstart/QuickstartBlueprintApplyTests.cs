using System.Text.Json;
using OffceOs.Application.Features.Agents;
using OffceOs.Application.Features.AgentRoutines;
using OffceOs.Application.Features.Providers;
using OffceOs.Application.Features.Quickstart;
using OffceOs.Database;
using OffceOs.Domain.Common.Primitives;
using OffceOs.Domain.Common.ValueObjects;
using OffceOs.Domain.Features.AgentRoutines;
using OffceOs.Domain.Features.Agents;
using OffceOs.Domain.Features.Context;
using OffceOs.Domain.Features.Providers;
using OffceOs.Infrastructure.Features.AgentRoutines;
using OffceOs.Infrastructure.Features.Agents;
using OffceOs.Infrastructure.Features.Channels;
using OffceOs.Infrastructure.Features.Context;
using OffceOs.Infrastructure.Features.Providers;
using OffceOs.Tests.Shared;
using Xunit;

namespace OffceOs.Tests.Quickstart;

public sealed class QuickstartBlueprintApplyTests
{
    [Fact]
    public async Task ApplyAsync_creates_workspace_resources_agent_attachments_and_routines_from_multi_file_yaml()
    {
        await using var db = WorkspaceTestHarness.CreateDb("quickstart-blueprint-apply");
        var ownerId = Guid.NewGuid();
        await WorkspaceTestHarness.SeedUserAsync(db, ownerId, "owner@example.com");
        var harness = WorkspaceTestHarness.Create(db);
        var workspace = await harness.Workspaces.GetCurrentAsync(ownerId);
        var service = CreateService(db, harness);

        var result = await service.ApplyAsync(
            new QuickstartBlueprintApplyRequest(
                [
                    new QuickstartFileRequest(
                        "workspace.yaml",
                        """
                        kind: workspace
                        resources:
                          browsers:
                            - key: qa_browser
                              display_name: QA Browser
                          memory_stores:
                            - key: support_memory
                              display_name: Support Memory
                        agents:
                          - key: support_planner
                            file: agents/support-planner.yaml
                        """),
                    new QuickstartFileRequest(
                        "agents/support-planner.yaml",
                        """
                        kind: agent
                        key: support_planner
                        name: Support planner
                        description: Plans support work from memory and browser state.
                        model: gpt-4o-mini
                        system: |-
                          Plan support work and verify customer-facing pages in the browser.
                        tools:
                          - type: agent_toolset_20260401
                          - type: browser_toolset
                        resources:
                          - type: browser
                            ref: qa_browser
                            access_mode: read_write
                            instructions: Use this browser for UI checks.
                          - type: memory_store
                            ref: support_memory
                            access_mode: read_only
                            instructions: Read support policy notes before planning.
                        routines:
                          - name: Daily support sweep
                            prompt: Review support blockers and summarize the highest priority work.
                            schedule_triggers:
                              - name: Weekday morning
                                expression: "0 9 * * 1-5"
                        metadata:
                          template: quickstart-blueprint-test
                        """),
                ],
                null,
                null),
            ownerId,
            workspace.Id);

        var createdAgent = Assert.Single(result.Agents);
        Assert.Equal("agents/support-planner.yaml", createdAgent.FilePath);
        Assert.Equal("Support planner", createdAgent.Name);

        var agent = await harness.Agents.GetByAsync(new AgentFilter { Id = createdAgent.Id, WorkspaceId = workspace.Id });
        Assert.NotNull(agent);
        var browsers = await new AgentResourceRepository(db).ListBrowserResourcesAsync(null, workspace.Id);
        var browser = Assert.Single(browsers);
        Assert.Equal("QA Browser", browser.DisplayName);
        Assert.Equal(agent!.Id, browser.CurrentAgentId);

        var memoryStores = await new MemoryStoreRepository(db).ListAsync(null, workspace.Id);
        var memoryStore = Assert.Single(memoryStores);
        Assert.Equal("Support Memory", memoryStore.DisplayName);

        var session = await new AgentSessionRepository(db).GetByAsync(new AgentSessionFilter
        {
            AgentId = agent.Id,
            Status = SessionStatus.Active,
        });
        var attachments = await new AgentResourceRepository(db).ListSessionAttachmentsAsync(session!.Id);
        Assert.Contains(attachments, attachment =>
            attachment.ResourceType == AgentResourceKinds.Browser
            && attachment.ResourceId == browser.Id
            && attachment.AccessMode == AgentResourceAccessModes.ReadWrite);
        Assert.Contains(attachments, attachment =>
            attachment.ResourceType == AgentResourceKinds.MemoryStore
            && attachment.ResourceId == memoryStore.Id
            && attachment.AccessMode == AgentResourceAccessModes.ReadOnly);

        var routines = await new AgentRoutineRepository(db).ListAsync(new AgentRoutineFilter { AgentId = agent.Id });
        var routine = Assert.Single(routines);
        Assert.Equal("Daily support sweep", routine.Name);
        var trigger = Assert.Single(routine.Triggers);
        Assert.Equal(AgentRoutineTriggerKinds.Schedule, trigger.Kind);
    }

    [Fact]
    public async Task ApplyAsync_keeps_single_agent_yaml_as_default_creation_path()
    {
        await using var db = WorkspaceTestHarness.CreateDb("quickstart-single-agent-default");
        var ownerId = Guid.NewGuid();
        await WorkspaceTestHarness.SeedUserAsync(db, ownerId, "owner@example.com");
        var harness = WorkspaceTestHarness.Create(db);
        var workspace = await harness.Workspaces.GetCurrentAsync(ownerId);
        var service = CreateService(db, harness);

        var result = await service.ApplyAsync(
            new QuickstartBlueprintApplyRequest(
                [
                    new QuickstartFileRequest(
                        "agent.yaml",
                        """
                        name: Single default agent
                        description: Default single-agent quickstart template.
                        model: gpt-4o-mini
                        system: Keep the single-agent path simple.
                        tools:
                          - type: agent_toolset_20260401
                        """),
                ],
                null,
                null),
            ownerId,
            workspace.Id);

        var createdAgent = Assert.Single(result.Agents);
        var agent = await harness.Agents.GetByAsync(new AgentFilter { Id = createdAgent.Id, WorkspaceId = workspace.Id });

        Assert.NotNull(agent);
        Assert.Equal("Single default agent", agent!.Name);
        Assert.Equal("agent.yaml", createdAgent.FilePath);
    }

    [Fact]
    public async Task ApplyAsync_rejects_duplicate_workspace_resource_keys()
    {
        await using var db = WorkspaceTestHarness.CreateDb("quickstart-duplicate-resource");
        var ownerId = Guid.NewGuid();
        await WorkspaceTestHarness.SeedUserAsync(db, ownerId, "owner@example.com");
        var harness = WorkspaceTestHarness.Create(db);
        var workspace = await harness.Workspaces.GetCurrentAsync(ownerId);
        var service = CreateService(db, harness);

        var ex = await Assert.ThrowsAsync<InvalidOperationException>(() => service.ApplyAsync(
            new QuickstartBlueprintApplyRequest(
                [
                    new QuickstartFileRequest(
                        "workspace.yaml",
                        """
                        kind: workspace
                        resources:
                          browsers:
                            - key: shared
                              display_name: Browser
                          memory_stores:
                            - key: shared
                              display_name: Memory
                        agents:
                          - key: support
                            file: agents/support.yaml
                        """),
                    new QuickstartFileRequest(
                        "agents/support.yaml",
                        """
                        kind: agent
                        name: Support
                        model: gpt-4o-mini
                        system: Support.
                        tools:
                          - type: agent_toolset_20260401
                        """),
                ],
                null,
                null),
            ownerId,
            workspace.Id));

        Assert.Contains("Duplicate workspace resource key", ex.Message);
    }

    private static QuickstartAgentService CreateService(EaosDbContext db, WorkspaceTestHarness harness) =>
        new(
            new FakeProviderService(providers:
            [
                new ProviderResult(
                    Guid.NewGuid(),
                    "openai",
                    "OpenAI",
                    true,
                    DateTime.UtcNow,
                    [
                        new ProviderModelResult("gpt-4o-mini", "GPT-4o Mini", 1),
                    ]),
            ]),
            new NoopProviderDispatchService(),
            harness.Integrations,
            new AgentResourceRepository(db),
            new AgentResourceService(new AgentResourceRepository(db), new AgentSessionRepository(db), harness.Agents),
            harness.AgentDashboard,
            new ChannelRepository(db),
            new MemoryStoreRepository(db),
            new FakeAgentRoutineService(),
            new AgentDefinitionParser(),
            new QuickstartBlueprintParser(new AgentDefinitionParser()),
            new SseResponseParser());

    private sealed class NoopProviderDispatchService : IProviderDispatchService
    {
        public Task<AgentResult<LlmDispatchResponse>> DispatchAsync(
            string provider,
            Guid? workspaceId,
            string model,
            JsonElement requestBody,
            CancellationToken ct = default) =>
            throw new NotSupportedException();
    }
}
