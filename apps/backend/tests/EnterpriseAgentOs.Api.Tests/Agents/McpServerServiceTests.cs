using EnterpriseAgentOs.Application.Features.Mcp;
using EnterpriseAgentOs.Domain.Features.Management;
using EnterpriseAgentOs.Domain.Features.Mcp;
using EnterpriseAgentOs.Infrastructure.Common.Configuration;
using EnterpriseAgentOs.Infrastructure.Common.Security;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace EnterpriseAgentOs.Api.Tests.Agents;

public sealed class McpServerServiceTests
{
    [Fact]
    public async Task RegisterAsync_adds_custom_server_to_catalog()
    {
        var service = CreateService();

        await service.RegisterAsync(new McpServerRecord
        {
            Name = "custom-server",
            Title = "Custom Server",
            TransportType = McpTransportType.Stdio,
            Command = "npx",
            Args = """["-y","custom-mcp"]""",
            Category = "custom",
        });

        var all = await service.ListAsync();
        var custom = Assert.Single(all, s => s.Name == "custom-server");
        Assert.Equal("Custom Server", custom.Title);
        Assert.False(custom.IsBuiltin);
    }

    [Fact]
    public async Task RegisterAsync_updates_existing_custom_server()
    {
        var service = CreateService();

        await service.RegisterAsync(CustomServer(command: "npx"));
        await service.RegisterAsync(CustomServer(command: "uvx"));

        var custom = await service.GetAsync("custom-server");
        Assert.NotNull(custom);
        Assert.Equal("uvx", custom.Command);
    }

    [Fact]
    public async Task RegisterAsync_rejects_builtin_name_collision()
    {
        var service = CreateService();

        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            service.RegisterAsync(CustomServer(name: "github")));
    }

    [Fact]
    public async Task DeleteAsync_removes_custom_server_and_bindings()
    {
        var agents = new FakeAgentMcpServerRepository();
        var service = CreateService(agentServers: agents);

        await service.RegisterAsync(CustomServer());
        await service.AssignToAgentAsync(Guid.NewGuid(), "custom-server");
        await service.DeleteAsync("custom-server");

        Assert.Null(await service.GetAsync("custom-server"));
        Assert.Empty(agents.AssignedServerNames);
    }

    [Fact]
    public async Task DeleteAsync_rejects_builtin_server()
    {
        var service = CreateService();

        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            service.DeleteAsync("github"));
    }

    [Fact]
    public async Task SaveCredentialAsync_marks_credential_server_configured()
    {
        var service = CreateService();

        await service.RegisterAsync(CustomServer(
            credentialFieldsJson:
            """[{"name":"API_KEY","label":"API Key","type":"password","required":true}]"""));
        await service.SaveCredentialAsync("custom-server", new() { ["API_KEY"] = "secret" });

        var custom = await service.GetAsync("custom-server");
        Assert.NotNull(custom);
        Assert.True(custom.CredentialConfigured);
    }

    private static McpServerService CreateService(
        FakeAgentMcpServerRepository? agentServers = null,
        FakeMcpServerRepository? servers = null,
        FakeMcpCredentialRepository? credentials = null)
    {
        var keyRingPath = Path.Combine(Path.GetTempPath(), $"eaos-test-keys-{Guid.NewGuid():N}");
        var protector = new CredentialProtector(
            DataProtectionProvider.Create(new DirectoryInfo(keyRingPath)));

        return new McpServerService(
            agentServers ?? new FakeAgentMcpServerRepository(),
            servers ?? new FakeMcpServerRepository(),
            credentials ?? new FakeMcpCredentialRepository(),
            new FakeOAuthTokenRepository(),
            protector,
            new GoogleOAuthConfig(),
            NullLogger<McpServerService>.Instance);
    }

    private static McpServerRecord CustomServer(
        string name = "custom-server",
        string command = "npx",
        string? credentialFieldsJson = null) => new()
    {
        Name = name,
        Title = "Custom Server",
        TransportType = McpTransportType.Stdio,
        Command = command,
        Args = """["-y","custom-mcp"]""",
        Category = "custom",
        CredentialFieldsJson = credentialFieldsJson,
    };

    private sealed class FakeMcpServerRepository : IMcpServerRepository
    {
        private readonly Dictionary<string, McpServerRecord> _servers = new(StringComparer.OrdinalIgnoreCase);

        public Task<IReadOnlyList<McpServerRecord>> ListAsync(CancellationToken ct = default)
            => Task.FromResult<IReadOnlyList<McpServerRecord>>(_servers.Values.ToList());

        public Task<McpServerRecord?> GetByNameAsync(string name, CancellationToken ct = default)
            => Task.FromResult(_servers.GetValueOrDefault(name));

        public Task<McpServerRecord> UpsertAsync(McpServerRecord server, CancellationToken ct = default)
        {
            var saved = Copy(server);
            _servers[server.Name] = saved;
            return Task.FromResult(saved);
        }

        public Task DeleteAsync(string name, CancellationToken ct = default)
        {
            _servers.Remove(name);
            return Task.CompletedTask;
        }

        private static McpServerRecord Copy(McpServerRecord server) => new()
        {
            Id = server.Id,
            Name = server.Name,
            Title = server.Title,
            Description = server.Description,
            TransportType = server.TransportType,
            Command = server.Command,
            Args = server.Args,
            Url = server.Url,
            Logo = server.Logo,
            Category = server.Category,
            CredentialFieldsJson = server.CredentialFieldsJson,
            Subtitle = server.Subtitle,
            AuthorName = server.AuthorName,
            AuthorUrl = server.AuthorUrl,
            DocumentationUrl = server.DocumentationUrl,
            RepositoryUrl = server.RepositoryUrl,
            ToolsJson = server.ToolsJson,
            IsBuiltin = false,
            CreatedAt = server.CreatedAt,
        };
    }

    private sealed class FakeAgentMcpServerRepository : IAgentMcpServerRepository
    {
        private readonly Dictionary<Guid, HashSet<string>> _assigned = new();

        public IReadOnlyList<string> AssignedServerNames => _assigned.Values.SelectMany(v => v).ToList();

        public Task<IReadOnlyList<string>> ListServerNamesForAgentAsync(Guid agentId, CancellationToken ct = default)
            => Task.FromResult<IReadOnlyList<string>>(
                _assigned.TryGetValue(agentId, out var names) ? names.ToList() : []);

        public Task AssignAsync(Guid agentId, string mcpServerName, CancellationToken ct = default)
        {
            if (!_assigned.TryGetValue(agentId, out var names))
                _assigned[agentId] = names = new(StringComparer.OrdinalIgnoreCase);
            names.Add(mcpServerName);
            return Task.CompletedTask;
        }

        public Task UnassignAsync(Guid agentId, string mcpServerName, CancellationToken ct = default)
        {
            if (_assigned.TryGetValue(agentId, out var names))
                names.Remove(mcpServerName);
            return Task.CompletedTask;
        }

        public Task UnassignServerFromAllAgentsAsync(string mcpServerName, CancellationToken ct = default)
        {
            foreach (var names in _assigned.Values)
                names.Remove(mcpServerName);
            return Task.CompletedTask;
        }
    }

    private sealed class FakeMcpCredentialRepository : IMcpCredentialRepository
    {
        private readonly Dictionary<string, McpCredentialRecord> _credentials = new(StringComparer.OrdinalIgnoreCase);

        public Task<McpCredentialRecord?> GetByAsync(McpCredentialFilter filter, CancellationToken ct = default)
            => Task.FromResult(filter.ServerName is null ? null : _credentials.GetValueOrDefault(filter.ServerName));

        public Task UpsertAsync(McpCredentialRecord credential, CancellationToken ct = default)
        {
            _credentials[credential.McpServerName] = credential;
            return Task.CompletedTask;
        }

        public Task DeleteAsync(string serverName, CancellationToken ct = default)
        {
            _credentials.Remove(serverName);
            return Task.CompletedTask;
        }
    }

    private sealed class FakeOAuthTokenRepository : IOAuthTokenRepository
    {
        public Task<OAuthTokenRecord?> GetByAsync(OAuthTokenFilter filter, CancellationToken ct = default)
            => Task.FromResult<OAuthTokenRecord?>(null);

        public Task UpsertAsync(OAuthTokenRecord token, CancellationToken ct = default)
            => Task.CompletedTask;
    }
}
