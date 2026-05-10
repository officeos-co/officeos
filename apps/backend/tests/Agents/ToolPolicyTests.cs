using System.Text.Json;
using OffceOs.Application.Features.Agents;
using OffceOs.Application.Features.Context;
using OffceOs.Domain.Common.Primitives;
using OffceOs.Domain.Events;
using OffceOs.Domain.Features.Agents;
using OffceOs.Domain.Features.Context;
using OffceOs.Domain.Features.Integrations;
using OffceOs.Domain.Features.Management;
using MediatR;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace OffceOs.Tests.Agents;

public sealed class ToolPolicyTests
{
    [Fact]
    public async Task Organization_policy_filters_builtin_network_write_and_integration_tool_definitions()
    {
        var agentId = Guid.NewGuid();
        var workspaceId = Guid.NewGuid();
        var factory = CreateFactory(new OrganizationPolicyProfileRecord
        {
            OrganizationId = Guid.NewGuid(),
            ShellToolsEnabled = false,
            NetworkToolsEnabled = false,
            FileWriteToolsEnabled = false,
            DeniedIntegrationsJson = """["salesforce"]""",
        });
        var integrations = new[]
        {
            new IntegrationDefinitionRecord
            {
                Name = "salesforce",
                Title = "Salesforce",
                ToolsJson = """[{"name":"query","description":"Query records","parameters":{"type":"object","properties":{}}}]""",
            },
        };

        await using var registry = await factory.CreateAsync(
            new FakeAgentSandbox(),
            "sandbox",
            "http://sandbox",
            agentId,
            workspaceId,
            "correlation",
            integrations,
            _ => Task.FromResult(new Dictionary<string, string>()),
            CancellationToken.None);

        var toolNames = registry.Tools.Select(tool => tool.Name).ToHashSet(StringComparer.Ordinal);

        Assert.DoesNotContain("shell", toolNames);
        Assert.DoesNotContain("file_write", toolNames);
        Assert.DoesNotContain("file_edit", toolNames);
        Assert.DoesNotContain("http_request", toolNames);
        Assert.DoesNotContain("web_fetch", toolNames);
        Assert.DoesNotContain("salesforce__query", toolNames);
        Assert.Contains("file_read", toolNames);
        Assert.Contains("tool_search", toolNames);
    }

    [Fact]
    public async Task Allowed_integration_policy_exposes_only_matching_integration_tool_definitions()
    {
        var factory = CreateFactory(new OrganizationPolicyProfileRecord
        {
            OrganizationId = Guid.NewGuid(),
            AllowedIntegrationsJson = """["google-docs"]""",
        });
        var integrations = new[]
        {
            new IntegrationDefinitionRecord
            {
                Name = "google-docs",
                Title = "Google Docs",
                ToolsJson = """[{"name":"create_document","description":"Create a document","parameters":{"type":"object","properties":{}}}]""",
            },
            new IntegrationDefinitionRecord
            {
                Name = "salesforce",
                Title = "Salesforce",
                ToolsJson = """[{"name":"query","description":"Query records","parameters":{"type":"object","properties":{}}}]""",
            },
        };

        await using var registry = await factory.CreateAsync(
            new FakeAgentSandbox(),
            "sandbox",
            "http://sandbox",
            Guid.NewGuid(),
            Guid.NewGuid(),
            "correlation",
            integrations,
            _ => Task.FromResult(new Dictionary<string, string>()),
            CancellationToken.None);

        var toolNames = registry.Tools.Select(tool => tool.Name).ToHashSet(StringComparer.Ordinal);

        Assert.Contains("google_docs__create_document", toolNames);
        Assert.DoesNotContain("salesforce__query", toolNames);
    }

    private static ToolRegistryFactory CreateFactory(OrganizationPolicyProfileRecord? policy)
        => new(
            new FakeAgentMemoryService(),
            new FakeAgentCronJobRepository(),
            new FakeAgentRunRepository(),
            new AgentTaskStore(),
            new ThrowingIntegrationClientManager(),
            new NoBrowserToolContextFactory(),
            new EmptyAgentToolPermissionRepository(),
            new FakeOrganizationPolicyService(policy),
            new FakeIntegrationExecutionService(),
            new TurnEventPublisher(new NoopPublisher()),
            NullLogger<ToolRegistryFactory>.Instance);

    private sealed class FakeOrganizationPolicyService : IOrganizationPolicyService
    {
        private readonly OrganizationPolicyProfileRecord? _policy;

        public FakeOrganizationPolicyService(OrganizationPolicyProfileRecord? policy) => _policy = policy;

        public Task<OrganizationPolicyProfileRecord?> GetEffectiveForWorkspaceAsync(Guid? workspaceId, CancellationToken ct = default) =>
            Task.FromResult(_policy);

        public Task<OrganizationPolicyProfileRecord> GetOrCreateAsync(Guid actorUserId, Guid organizationId, CancellationToken ct = default) =>
            Task.FromResult(_policy ?? OrganizationPolicyProfileRecord.Default(organizationId));

        public Task<OrganizationPolicyProfileRecord> UpdateAsync(Guid actorUserId, OrganizationPolicyProfileRecord profile, CancellationToken ct = default) =>
            Task.FromResult(profile);
    }

    private sealed class FakeAgentMemoryService : IAgentMemoryService
    {
        public Task StoreAsync(Guid agentId, string key, string content, CancellationToken ct = default) => Task.CompletedTask;

        public Task<IReadOnlyList<AgentMemoryRecord>> RecallAsync(Guid agentId, string query, int limit, CancellationToken ct = default) =>
            Task.FromResult<IReadOnlyList<AgentMemoryRecord>>([]);

        public Task<bool> ForgetAsync(Guid agentId, string key, CancellationToken ct = default) => Task.FromResult(false);
    }

    private sealed class FakeAgentCronJobRepository : IAgentCronJobRepository
    {
        public Task<IReadOnlyList<AgentCronJobRecord>> ListAsync(Guid agentId, CancellationToken ct = default) =>
            Task.FromResult<IReadOnlyList<AgentCronJobRecord>>([]);

        public Task<IReadOnlyList<AgentCronJobWithAgentRecord>> ListForOwnerAsync(Guid? ownerId, Guid? workspaceId = null, CancellationToken ct = default) =>
            Task.FromResult<IReadOnlyList<AgentCronJobWithAgentRecord>>([]);

        public Task<IReadOnlyList<AgentCronJobRecord>> ListAllEnabledAsync(CancellationToken ct = default) =>
            Task.FromResult<IReadOnlyList<AgentCronJobRecord>>([]);

        public Task<AgentCronJobRecord?> GetByAsync(AgentCronJobFilter filter, CancellationToken ct = default) =>
            Task.FromResult<AgentCronJobRecord?>(null);

        public Task<AgentCronJobWithAgentRecord?> GetForOwnerAsync(Guid id, Guid? ownerId, Guid? workspaceId = null, CancellationToken ct = default) =>
            Task.FromResult<AgentCronJobWithAgentRecord?>(null);

        public Task<AgentCronJobRecord> CreateAsync(Guid agentId, string name, string expression, string prompt, CancellationToken ct = default) =>
            Task.FromResult(AgentCronJobRecord.Create(agentId, name, expression, prompt));

        public Task UpdateAsync(AgentCronJobRecord record, CancellationToken ct = default) => Task.CompletedTask;

        public Task SetEnabledAsync(Guid id, bool enabled, CancellationToken ct = default) => Task.CompletedTask;

        public Task<bool> DeleteAsync(Guid id, CancellationToken ct = default) => Task.FromResult(false);
    }

    private sealed class FakeAgentRunRepository : IAgentRunRepository
    {
        public Task<AgentRunRecord> CreateAsync(AgentRunRecord run, CancellationToken ct = default) => Task.FromResult(run);

        public Task<AgentRunRecord?> GetByAsync(AgentRunFilter filter, CancellationToken ct = default) => Task.FromResult<AgentRunRecord?>(null);

        public Task<IReadOnlyList<AgentRunRecord>> ListForAgentAsync(Guid agentId, Guid? parentRunId = null, CancellationToken ct = default) =>
            Task.FromResult<IReadOnlyList<AgentRunRecord>>([]);

        public Task UpdateAsync(AgentRunRecord run, CancellationToken ct = default) => Task.CompletedTask;
    }

    private sealed class ThrowingIntegrationClientManager : IIntegrationClientManager
    {
        public Task<IntegrationConnectionResult> ConnectAsync(
            IntegrationDefinitionRecord server,
            Dictionary<string, string> credentials,
            CancellationToken ct = default)
            => throw new InvalidOperationException("Lazy catalog integrations should not connect while building tool definitions.");
    }

    private sealed class NoBrowserToolContextFactory : IBrowserToolContextFactory
    {
        public Task<BrowserToolContext?> CreateForTurnAsync(CancellationToken ct = default) =>
            Task.FromResult<BrowserToolContext?>(null);

        public Task<BrowserToolContext> CreateCatalogAsync(CancellationToken ct = default) =>
            throw new NotSupportedException();
    }

    private sealed class EmptyAgentToolPermissionRepository : IAgentToolPermissionRepository
    {
        public Task<IReadOnlyList<AgentToolPermissionRecord>> ListForAgentAsync(Guid agentId, CancellationToken ct = default) =>
            Task.FromResult<IReadOnlyList<AgentToolPermissionRecord>>([]);

        public Task UpsertAsync(Guid agentId, string skillName, string toolName, ToolPermission permission, CancellationToken ct = default) =>
            Task.CompletedTask;

        public Task SetManyAsync(Guid agentId, IReadOnlyList<AgentToolPermissionRecord> entries, CancellationToken ct = default) =>
            Task.CompletedTask;
    }

    private sealed class FakeIntegrationExecutionService : IIntegrationExecutionService
    {
        public Task<JsonElement> ExecuteAsync(IntegrationExecuteRequest request, CancellationToken ct = default) =>
            Task.FromResult(JsonSerializer.SerializeToElement(new { ok = true }));
    }

    private sealed class FakeAgentSandbox : IAgentSandbox
    {
        public Task<AgentSandboxDeployment> CreateAsync(
            Guid agentId,
            IReadOnlyDictionary<string, string> environment,
            IReadOnlyDictionary<string, string> metadata,
            CancellationToken ct = default)
            => Task.FromResult(new AgentSandboxDeployment("sandbox", "http://sandbox"));

        public Task<AgentResult<AgentSandboxCommandResult>> ExecuteAsync(
            string sandboxId,
            string serviceUrl,
            string command,
            TimeSpan timeout,
            CancellationToken ct = default)
            => Task.FromResult<AgentResult<AgentSandboxCommandResult>>(new AgentSandboxCommandResult("", 0));

        public Task<AgentResult<string>> ReadFileAsync(string sandboxId, string serviceUrl, string path, CancellationToken ct = default) =>
            Task.FromResult<AgentResult<string>>(string.Empty);

        public Task<AgentResult<bool>> WriteFileAsync(string sandboxId, string serviceUrl, string path, string content, CancellationToken ct = default) =>
            Task.FromResult<AgentResult<bool>>(true);

        public Task<bool> TerminateAsync(string sandboxId, CancellationToken ct = default) => Task.FromResult(true);
    }

    private sealed class NoopPublisher : IPublisher
    {
        public Task Publish(object notification, CancellationToken cancellationToken = default) => Task.CompletedTask;

        public Task Publish<TNotification>(TNotification notification, CancellationToken cancellationToken = default)
            where TNotification : INotification
            => Task.CompletedTask;
    }
}
