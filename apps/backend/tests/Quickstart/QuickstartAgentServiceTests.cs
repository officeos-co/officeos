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
using OffceOs.Domain.Features.Channels;
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

public sealed class QuickstartAgentServiceTests
{
    [Fact]
    public async Task ChatAsync_injects_full_workspace_context_and_returns_valid_yaml()
    {
        await using var db = WorkspaceTestHarness.CreateDb("quickstart-context");
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
            BrowserResourceRecord.Create(ownerId, workspace.Id, "QA Browser"));
        var memory = await new MemoryStoreRepository(db).CreateAsync(
            MemoryStoreRecord.Create(ownerId, workspace.Id, "Support Memory"));
        var channel = await new ChannelRepository(db).CreateConnectionAsync(
            ChannelConnectionRecord.Create(ChannelType.Slack, "Support Slack", ownerId, workspace.Id));
        var existingAgent = await harness.AgentDashboard.CreateAsync(
            new CreateDashboardAgentRequest(
                "Existing Agent",
                "openai",
                "gpt-4o-mini",
                null,
                null,
                null,
                null,
                null,
                null,
                null),
            ownerId,
            workspace.Id);
        await new AgentRoutineRepository(db).UpsertAsync(AgentRoutineRecord.Create(
            existingAgent.Id,
            "Existing sweep",
            "Summarize existing work."));
        var dispatch = new RecordingProviderDispatchService(
            """
            name: Support planner
            description: Plans support work from Asana.
            model: gpt-4o-mini
            system: Plan support work.
            mcp_servers:
              - name: asana
                type: registered
            tools:
              - type: agent_toolset_20260401
              - type: mcp_toolset
                mcp_server_name: asana
                default_config:
                  permission_policy:
                    type: always_allow
            metadata:
              template: quickstart-context-test
            """);
        var service = CreateService(db, harness, dispatch);

        var result = await service.ChatAsync(
            new QuickstartAgentChatRequest(
                "Build a support planning agent that uses Asana and the workspace resources.",
                null,
                null,
                null,
                null),
            ownerId,
            workspace.Id);

        var requestText = dispatch.RequestBody.GetRawText();
        Assert.Contains("gpt-4o-mini", requestText);
        Assert.Contains("gpt-4o", requestText);
        Assert.Contains("Configured MCP servers", requestText);
        Assert.Contains("asana", requestText);
        Assert.Contains(browser.Id.ToString(), requestText);
        Assert.Contains(memory.Id.ToString(), requestText);
        Assert.Contains(channel.Id.ToString(), requestText);
        Assert.Contains("Existing sweep", requestText);
        Assert.Contains("resources:", requestText);
        Assert.Contains("routines:", requestText);
        Assert.Contains("Support planner", result.ConfigYaml);
        Assert.Contains("\"name\":\"Support planner\"", result.ConfigJson);
    }

    [Fact]
    public async Task ChatAsync_rejects_placeholder_requests_before_dispatch()
    {
        await using var db = WorkspaceTestHarness.CreateDb("quickstart-placeholder");
        var ownerId = Guid.NewGuid();
        await WorkspaceTestHarness.SeedUserAsync(db, ownerId, "owner@example.com");
        var harness = WorkspaceTestHarness.Create(db);
        var workspace = await harness.Workspaces.GetCurrentAsync(ownerId);
        var dispatch = new RecordingProviderDispatchService("name: Test\nmodel: gpt-4o-mini");
        var service = CreateService(db, harness, dispatch);

        var ex = await Assert.ThrowsAsync<InvalidOperationException>(() => service.ChatAsync(
            new QuickstartAgentChatRequest("test", null, null, null, null),
            ownerId,
            workspace.Id));

        Assert.Contains("concrete agent goal", ex.Message);
        Assert.Equal(0, dispatch.DispatchCount);
    }

    private static QuickstartAgentService CreateService(
        EaosDbContext db,
        WorkspaceTestHarness harness,
        RecordingProviderDispatchService dispatch) =>
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
                        new ProviderModelResult("gpt-4o", "GPT-4o", 15),
                    ]),
            ]),
            dispatch,
            harness.Integrations,
            new AgentResourceRepository(db),
            new ChannelRepository(db),
            new MemoryStoreRepository(db),
            new FakeAgentRoutineServiceWithRepository(new AgentRoutineRepository(db)),
            new AgentDefinitionParser(),
            new SseResponseParser());

    private sealed class FakeAgentRoutineServiceWithRepository : IAgentRoutineService
    {
        private readonly AgentRoutineRepository _agentRoutineRepository;

        public FakeAgentRoutineServiceWithRepository(AgentRoutineRepository agentRoutineRepository)
        {
            _agentRoutineRepository = agentRoutineRepository;
        }

        public Task<IReadOnlyList<AgentRoutineWithAgentRecord>> ListForOwnerAsync(Guid ownerId, Guid workspaceId, CancellationToken ct = default) =>
            _agentRoutineRepository.ListForOwnerAsync(null, workspaceId, ct);

        public Task<AgentRoutineWithAgentRecord?> GetForOwnerAsync(Guid id, Guid ownerId, Guid workspaceId, CancellationToken ct = default) =>
            _agentRoutineRepository.GetForOwnerAsync(id, null, workspaceId, ct);

        public Task<IReadOnlyList<AgentRoutineRecord>> ListForAgentAsync(Guid agentId, Guid ownerId, Guid workspaceId, CancellationToken ct = default) =>
            _agentRoutineRepository.ListAsync(new AgentRoutineFilter { AgentId = agentId }, ct);

        public Task<AgentRoutineCreateResult> CreateAsync(CreateAgentRoutineRequest request, Guid ownerId, Guid workspaceId, CancellationToken ct = default) =>
            throw new NotSupportedException();

        public Task<bool> SetEnabledAsync(Guid id, Guid ownerId, Guid workspaceId, bool enabled, CancellationToken ct = default) =>
            throw new NotSupportedException();

        public Task<bool> DeleteAsync(Guid id, Guid ownerId, Guid workspaceId, CancellationToken ct = default) =>
            throw new NotSupportedException();
    }

    private sealed class RecordingProviderDispatchService : IProviderDispatchService
    {
        private readonly string _yaml;

        public RecordingProviderDispatchService(string yaml)
        {
            _yaml = yaml;
        }

        public JsonElement RequestBody { get; private set; }
        public int DispatchCount { get; private set; }

        public Task<AgentResult<LlmDispatchResponse>> DispatchAsync(
            string provider,
            Guid? workspaceId,
            string model,
            JsonElement requestBody,
            CancellationToken ct = default)
        {
            DispatchCount++;
            RequestBody = requestBody.Clone();
            var content = JsonSerializer.Serialize(new
            {
                message = "Generated a support planner.",
                yaml = _yaml,
            });
            var chunk = JsonSerializer.Serialize(new
            {
                choices = new[]
                {
                    new
                    {
                        index = 0,
                        delta = new { content },
                    },
                },
            });
            return Task.FromResult<AgentResult<LlmDispatchResponse>>(new LlmDispatchResponse(
                HttpResponseFactory.SseResponse($"data: {chunk}\n\ndata: [DONE]\n\n"),
                model));
        }
    }
}
