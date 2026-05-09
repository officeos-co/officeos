using EnterpriseAgentOs.Application.Features.Agents.Integrations;
using EnterpriseAgentOs.Domain.Common.ValueObjects;
using EnterpriseAgentOs.Domain.Features.Agents;
using EnterpriseAgentOs.Domain.Features.Agents.Integrations;
using EnterpriseAgentOs.Domain.Features.Management;
using EnterpriseAgentOs.Configuration;
using EnterpriseAgentOs.Infrastructure.Common.Security;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace EnterpriseAgentOs.Api.Tests.Agents;

public sealed class McpServerServiceTests
{
    private static readonly Guid OwnerId = Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa");

    [Fact]
    public async Task RegisterAsync_adds_custom_server_to_owner_catalog()
    {
        var service = CreateService();

        await service.RegisterAsync(OwnerId, CustomServer());

        var all = await service.ListAsync(OwnerId);
        var custom = Assert.Single(all, s => s.Name == "custom-server");
        Assert.Equal("Custom Server", custom.Title);
        Assert.False(custom.IsBuiltin);
    }

    [Fact]
    public async Task RegisterAsync_updates_existing_custom_server()
    {
        var service = CreateService();

        await service.RegisterAsync(OwnerId, CustomServer(command: "npx"));
        await service.RegisterAsync(OwnerId, CustomServer(command: "uvx"));

        var custom = await service.GetAsync(OwnerId, "custom-server");
        Assert.NotNull(custom);
        Assert.Equal("uvx", custom.Command);
    }

    [Fact]
    public async Task SaveCredentialAsync_marks_custom_server_configured()
    {
        var service = CreateService();

        await service.RegisterAsync(OwnerId, CustomServer(
            credentialFieldsJson:
            """[{"name":"API_KEY","label":"API Key","type":"password","required":true}]"""));
        await service.SaveCredentialAsync(OwnerId, "custom-server", new() { ["API_KEY"] = "secret" });

        var custom = await service.GetAsync(OwnerId, "custom-server");
        Assert.NotNull(custom);
        Assert.True(custom.CredentialConfigured);
    }

    [Fact]
    public async Task DeleteAsync_removes_custom_server_and_owner_bindings()
    {
        var agents = new FakeAgentIntegrationRepository();
        var agentId = Guid.Parse("bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb");
        var service = CreateService(agentServers: agents);

        await service.RegisterAsync(OwnerId, CustomServer());
        await service.AssignToAgentAsync(agentId, "custom-server", OwnerId);
        await service.DeleteAsync(OwnerId, "custom-server");

        Assert.Null(await service.GetAsync(OwnerId, "custom-server"));
        Assert.Empty(agents.AssignedIntegrationNames);
    }

    private static IntegrationDefinitionService CreateService(
        FakeAgentIntegrationRepository? agentServers = null,
        FakeIntegrationDefinitionRepository? servers = null,
        FakeIntegrationCredentialRepository? credentials = null)
    {
        var keyRingPath = Path.Combine(Path.GetTempPath(), $"eaos-test-keys-{Guid.NewGuid():N}");
        var protector = new CredentialProtector(
            DataProtectionProvider.Create(new DirectoryInfo(keyRingPath)));

        return new IntegrationDefinitionService(
            agentServers ?? new FakeAgentIntegrationRepository(),
            new FakeAgentRepository(),
            servers ?? new FakeIntegrationDefinitionRepository(),
            credentials ?? new FakeIntegrationCredentialRepository(),
            new FakeOAuthTokenRepository(),
            protector,
            new GoogleOAuthConfig(),
            NullLogger<IntegrationDefinitionService>.Instance);
    }

    private static IntegrationDefinitionRecord CustomServer(
        string name = "custom-server",
        string command = "npx",
        string? credentialFieldsJson = null) => new()
    {
        Name = name,
        Title = "Custom Server",
        TransportType = IntegrationTransportType.Stdio,
        Command = command,
        Args = """["-y","custom-mcp"]""",
        Category = "custom",
        CredentialFieldsJson = credentialFieldsJson,
    };

    private sealed class FakeIntegrationDefinitionRepository : IIntegrationDefinitionRepository
    {
        private readonly Dictionary<(Guid OwnerId, string Name), IntegrationDefinitionRecord> _servers = new();

        public Task<IReadOnlyList<IntegrationDefinitionRecord>> ListAsync(Guid ownerId, CancellationToken ct = default)
            => Task.FromResult<IReadOnlyList<IntegrationDefinitionRecord>>(
                _servers.Where(kvp => kvp.Key.OwnerId == ownerId).Select(kvp => kvp.Value).ToList());

        public Task<IntegrationDefinitionRecord?> GetByNameAsync(Guid ownerId, string name, CancellationToken ct = default)
            => Task.FromResult(_servers.GetValueOrDefault((ownerId, name)));

        public Task<IntegrationDefinitionRecord> UpsertAsync(Guid ownerId, IntegrationDefinitionRecord server, CancellationToken ct = default)
        {
            var saved = server with { OwnerId = ownerId, IsBuiltin = false };
            _servers[(ownerId, server.Name)] = saved;
            return Task.FromResult(saved);
        }

        public Task DeleteAsync(Guid ownerId, string name, CancellationToken ct = default)
        {
            _servers.Remove((ownerId, name));
            return Task.CompletedTask;
        }
    }

    private sealed class FakeAgentIntegrationRepository : IAgentIntegrationRepository
    {
        private readonly Dictionary<Guid, HashSet<string>> _assigned = new();

        public IReadOnlyList<string> AssignedIntegrationNames => _assigned.Values.SelectMany(v => v).ToList();

        public Task<IReadOnlyList<string>> ListIntegrationNamesForAgentAsync(Guid agentId, CancellationToken ct = default)
            => Task.FromResult<IReadOnlyList<string>>(
                _assigned.TryGetValue(agentId, out var names) ? names.ToList() : []);

        public Task AssignAsync(Guid agentId, string integrationName, CancellationToken ct = default)
        {
            if (!_assigned.TryGetValue(agentId, out var names))
                _assigned[agentId] = names = new(StringComparer.OrdinalIgnoreCase);
            names.Add(integrationName);
            return Task.CompletedTask;
        }

        public Task UnassignAsync(Guid agentId, string integrationName, CancellationToken ct = default)
        {
            if (_assigned.TryGetValue(agentId, out var names))
                names.Remove(integrationName);
            return Task.CompletedTask;
        }

        public Task UnassignIntegrationFromOwnerAgentsAsync(Guid ownerId, string integrationName, CancellationToken ct = default)
        {
            foreach (var names in _assigned.Values)
                names.Remove(integrationName);
            return Task.CompletedTask;
        }
    }

    private sealed class FakeIntegrationCredentialRepository : IIntegrationCredentialRepository
    {
        private readonly Dictionary<(Guid OwnerId, string IntegrationName), IntegrationCredentialRecord> _credentials = new();

        public Task<IntegrationCredentialRecord?> GetByAsync(IntegrationCredentialFilter filter, CancellationToken ct = default)
        {
            if (!filter.OwnerId.HasValue || filter.IntegrationName is null)
                return Task.FromResult<IntegrationCredentialRecord?>(null);

            return Task.FromResult(_credentials.GetValueOrDefault((filter.OwnerId.Value, filter.IntegrationName)));
        }

        public Task UpsertAsync(IntegrationCredentialRecord credential, CancellationToken ct = default)
        {
            _credentials[(credential.OwnerId, credential.IntegrationName)] = credential;
            return Task.CompletedTask;
        }

        public Task DeleteAsync(Guid ownerId, string integrationName, CancellationToken ct = default)
        {
            _credentials.Remove((ownerId, integrationName));
            return Task.CompletedTask;
        }
    }

    private sealed class FakeAgentRepository : IAgentRepository
    {
        public Task<IReadOnlyList<AgentRecord>> ListAsync(AgentFilter filter, CancellationToken ct = default)
            => Task.FromResult<IReadOnlyList<AgentRecord>>([]);

        public Task<AgentRecord?> GetByAsync(AgentFilter filter, CancellationToken ct = default)
            => Task.FromResult<AgentRecord?>(new AgentRecord
            {
                Id = filter.Id ?? Guid.NewGuid(),
                OwnerId = filter.OwnerId ?? OwnerId,
                Name = "Test Agent",
                Provider = "openai",
                Status = AgentStatus.Running,
            });

        public Task AddAsync(AgentRecord record, CancellationToken ct = default) => Task.CompletedTask;
        public Task UpdateAsync(AgentRecord record, CancellationToken ct = default) => Task.CompletedTask;
        public Task<bool> SoftDeleteAsync(AgentFilter filter, CancellationToken ct = default) => Task.FromResult(true);
        public Task UpdateStatusAsync(AgentFilter filter, AgentStatus status, CancellationToken ct = default) => Task.CompletedTask;
        public Task HardDeleteAsync(AgentFilter filter, CancellationToken ct = default) => Task.CompletedTask;
    }

    private sealed class FakeOAuthTokenRepository : IOAuthTokenRepository
    {
        public Task<OAuthTokenRecord?> GetByAsync(OAuthTokenFilter filter, CancellationToken ct = default)
            => Task.FromResult<OAuthTokenRecord?>(null);

        public Task UpsertAsync(OAuthTokenRecord token, CancellationToken ct = default)
            => Task.CompletedTask;
    }
}
