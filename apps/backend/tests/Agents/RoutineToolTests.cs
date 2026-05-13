using System.Text.Json;
using OffceOs.Application.Features.Agents;
using OffceOs.Domain.Events;
using OffceOs.Domain.Features.Agents;
using OffceOs.Tests.Shared;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace OffceOs.Tests.Agents;

public sealed class RoutineToolTests
{
    [Fact]
    public async Task Default_registry_exposes_routine_self_management_tools()
    {
        await using var registry = await ToolRegistryTestFactory.CreateFactory(null).CreateAsync(new ToolRegistryRequest
        {
            Sandbox = new FakeAgentSandbox(),
            SandboxId = "sandbox",
            ServiceUrl = "http://sandbox",
            AgentId = Guid.NewGuid(),
            WorkspaceId = Guid.NewGuid(),
            OwnerId = Guid.NewGuid(),
            CorrelationId = "correlation",
            Integrations = [],
        }, CancellationToken.None);

        var toolNames = registry.Tools.Select(tool => tool.Name).ToHashSet(StringComparer.Ordinal);

        Assert.Contains("routine_create", toolNames);
        Assert.Contains("routine_list", toolNames);
        Assert.Contains("routine_delete", toolNames);
        Assert.False(registry.Tools.Single(tool => tool.Name == "routine_create").ShouldDefer);
    }

    [Fact]
    public async Task Agent_builtin_allow_list_does_not_remove_routine_self_management_tools()
    {
        var agentId = Guid.NewGuid();
        var parser = new AgentDefinitionParser();
        var definition = parser.CreateRecord(
            agentId,
            1,
            parser.Parse(
                """
                name: Narrow agent
                model: gpt-4o-mini
                tools:
                  - type: agent_toolset_20260401
                    default_config:
                      permission_policy:
                        type: allow_list
                        tools:
                          - file_read
                """),
            "openai",
            Guid.NewGuid());
        var factory = new ToolRegistryFactory(
            new FakeAgentMemoryService(),
            new FakeAgentRoutineRepository(),
            new FakeAgentRoutineService(),
            new FakeAgentRunRepository(),
            new AgentTaskStore(),
            new BrowserToolService(new NoBrowserToolContextFactory()),
            new SingleAgentDefinitionRepository(definition),
            parser,
            new FakeOrganizationPolicyService(),
            new FakeIntegrationRuntimeService(),
            new TurnEventPublisher(new NoopPublisher()),
            NullLogger<ToolRegistryFactory>.Instance);

        await using var registry = await factory.CreateAsync(new ToolRegistryRequest
        {
            Sandbox = new FakeAgentSandbox(),
            SandboxId = "sandbox",
            ServiceUrl = "http://sandbox",
            AgentId = agentId,
            WorkspaceId = Guid.NewGuid(),
            OwnerId = Guid.NewGuid(),
            CorrelationId = "correlation",
            Integrations = [],
        }, CancellationToken.None);

        var toolNames = registry.Tools.Select(tool => tool.Name).ToHashSet(StringComparer.Ordinal);

        Assert.Contains("file_read", toolNames);
        Assert.DoesNotContain("shell", toolNames);
        Assert.Contains("routine_create", toolNames);
        Assert.Contains("routine_list", toolNames);
        Assert.Contains("routine_delete", toolNames);
    }

    [Fact]
    public async Task Routine_create_tool_creates_schedule_api_and_github_triggers_for_current_agent()
    {
        var service = new FakeAgentRoutineService();
        var agentId = Guid.NewGuid();
        var ownerId = Guid.NewGuid();
        var workspaceId = Guid.NewGuid();
        var tool = new RoutineCreateTool(service, agentId, ownerId, workspaceId);
        using var args = JsonDocument.Parse(
            """
            {
              "name": "Release watcher",
              "prompt": "Review release signals.",
              "schedule_triggers": [{ "name": "Weekday check", "expression": "*/5 * * * *" }],
              "api_triggers": [{ "name": "Deploy hook" }],
              "github_triggers": [{
                "name": "Pull requests",
                "owner": "acme",
                "repo": "platform",
                "events": ["pull_request"],
                "secret": "github-secret"
              }]
            }
            """);

        var validation = await tool.ValidateAsync(args.RootElement);
        var result = await tool.ExecuteAsync(args.RootElement);

        Assert.True(validation.IsValid);
        Assert.False(result.IsFailure);
        Assert.True(result.Value.Success);
        Assert.Contains("generated-secret", result.Value.Output);
        Assert.Equal(agentId, service.LastCreateRequest?.AgentId);
        Assert.Equal(ownerId, service.LastOwnerId);
        Assert.Equal(workspaceId, service.LastWorkspaceId);
        Assert.Single(service.LastCreateRequest!.ScheduleTriggers);
        Assert.Single(service.LastCreateRequest.ApiTriggers);
        Assert.Single(service.LastCreateRequest.GitHubTriggers);
    }

    [Fact]
    public async Task Routine_create_tool_rejects_empty_triggers_and_invalid_cron()
    {
        var tool = new RoutineCreateTool(new FakeAgentRoutineService(), Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid());
        using var emptyTriggers = JsonDocument.Parse("""{ "name": "No trigger", "prompt": "Do work." }""");
        using var invalidCron = JsonDocument.Parse(
            """
            {
              "name": "Bad schedule",
              "prompt": "Do work.",
              "schedule_triggers": [{ "name": "broken", "expression": "not cron" }]
            }
            """);

        var emptyResult = await tool.ValidateAsync(emptyTriggers.RootElement);
        var cronResult = await tool.ValidateAsync(invalidCron.RootElement);

        Assert.False(emptyResult.IsValid);
        Assert.Contains("At least one", emptyResult.Message);
        Assert.False(cronResult.IsValid);
        Assert.Contains("Invalid cron expression", cronResult.Message);
    }

    private sealed class SingleAgentDefinitionRepository : IAgentDefinitionRepository
    {
        private readonly AgentDefinitionRecord _definition;

        public SingleAgentDefinitionRepository(AgentDefinitionRecord definition)
        {
            _definition = definition;
        }

        public Task<AgentDefinitionRecord?> GetByAsync(AgentDefinitionFilter filter, CancellationToken ct = default) =>
            Task.FromResult(filter.AgentId == _definition.AgentId ? _definition : null);

        public Task<IReadOnlyList<AgentDefinitionRecord>> ListAsync(AgentDefinitionFilter filter, CancellationToken ct = default) =>
            Task.FromResult<IReadOnlyList<AgentDefinitionRecord>>([_definition]);

        public Task AddAsync(AgentDefinitionRecord definition, CancellationToken ct = default) =>
            Task.CompletedTask;

        public Task<int> GetNextVersionAsync(Guid agentId, CancellationToken ct = default) =>
            Task.FromResult(2);
    }
}
