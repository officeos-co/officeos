using OffceOs.Application.Features.Agents;
using OffceOs.Application.Features.Analytics;
using OffceOs.Application.Features.Channels;
using OffceOs.Application.Features.Integrations;
using OffceOs.Application.Features.Management;
using OffceOs.Configuration;
using OffceOs.Database;
using OffceOs.Database.Models;
using OffceOs.Domain.Common.ValueObjects;
using OffceOs.Domain.Features.Agents;
using OffceOs.Domain.Features.Analytics;
using OffceOs.Domain.Features.Channels;
using OffceOs.Domain.Features.Integrations;
using OffceOs.Domain.Features.Management;
using OffceOs.Infrastructure.Common.Security;
using OffceOs.Infrastructure.Features.Agents;
using OffceOs.Infrastructure.Features.Channels;
using OffceOs.Infrastructure.Features.Context;
using OffceOs.Infrastructure.Features.Integrations;
using OffceOs.Infrastructure.Features.Management;
using MediatR;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace OffceOs.Tests.Management;

public sealed class WorkspaceOrganizationEndToEndTests
{
    [Fact]
    public async Task Personal_workspace_lifecycle_supports_delete_recreate_and_agent_scoping()
    {
        await using var db = CreateDb();
        var userId = Guid.NewGuid();
        await SeedUserAsync(db, userId, "owner@example.com");

        var harness = CreateHarness(db);
        var personalDefault = await harness.Workspaces.GetCurrentAsync(userId);
        var scratch = await harness.Workspaces.CreateAsync(userId, "Scratch");

        await harness.Workspaces.SwitchAsync(userId, scratch.Id);
        Assert.True(await harness.Workspaces.DeleteAsync(userId, scratch.Id));

        var replacement = await harness.Workspaces.CreateAsync(userId, "Automation");
        await harness.Workspaces.SwitchAsync(userId, replacement.Id);

        var agent = await harness.AgentDashboard.CreateAsync(
            new CreateDashboardAgentRequest(
                "Workspace Agent",
                "openai",
                "gpt-4o-mini",
                null,
                null,
                null,
                null,
                null,
                null,
                null),
            userId,
            replacement.Id);

        var replacementAgents = await harness.Agents.ListAsync(new AgentFilter { WorkspaceId = replacement.Id });
        var defaultAgents = await harness.Agents.ListAsync(new AgentFilter { WorkspaceId = personalDefault.Id });
        var workspaces = await harness.Workspaces.ListAsync(userId);

        Assert.Contains(workspaces, workspace => workspace.Id == personalDefault.Id && workspace.OwnerKind == WorkspaceOwnerKind.Personal);
        Assert.DoesNotContain(workspaces, workspace => workspace.Id == scratch.Id);
        Assert.Single(replacementAgents);
        Assert.Equal(agent.Id, replacementAgents[0].Id);
        Assert.Empty(defaultAgents);
    }

    [Fact]
    public async Task Organization_flow_supports_roles_org_workspace_agents_and_workspace_wide_integrations()
    {
        await using var db = CreateDb();
        var ownerId = Guid.NewGuid();
        var memberId = Guid.NewGuid();
        await SeedUserAsync(db, ownerId, "owner@example.com");
        await SeedUserAsync(db, memberId, "member@example.com");

        var harness = CreateHarness(db);
        var overview = await harness.Organizations.GetOverviewAsync(ownerId, "owner@example.com", "Owner");
        var orgDefault = await harness.Workspaces.GetCurrentAsync(ownerId);
        var member = await harness.Organizations.InviteMemberAsync(
            ownerId,
            "owner@example.com",
            "Owner",
            "member@example.com",
            "Member");

        var memberWorkspaces = await harness.Workspaces.ListAsync(memberId);
        var memberOrgDefault = Assert.Single(memberWorkspaces, workspace => workspace.OrganizationId == overview.Organization.Id);

        var opsWorkspace = await harness.Workspaces.CreateOrganizationWorkspaceAsync(ownerId, overview.Organization.Id, "Ops");
        var opsMembership = await harness.Workspaces.AddMemberAsync(ownerId, opsWorkspace.Id, memberId, "Viewer");
        var upgradedMembership = await harness.Workspaces.UpdateMemberRoleAsync(ownerId, opsWorkspace.Id, memberId, "Editor");
        await harness.Workspaces.SwitchAsync(memberId, opsWorkspace.Id);

        await harness.Integrations.RegisterAsync(ownerId, opsWorkspace.Id, CustomIntegration());
        await harness.Integrations.SaveCredentialAsync(ownerId, opsWorkspace.Id, "org-docs", new() { ["API_KEY"] = "secret" });

        var memberVisibleIntegration = await harness.Integrations.GetAsync(memberId, "org-docs", opsWorkspace.Id);
        var memberPersonalWorkspace = Assert.Single(memberWorkspaces, workspace => workspace.OwnerKind == WorkspaceOwnerKind.Personal);
        var personalIntegration = await harness.Integrations.GetAsync(memberId, "org-docs", memberPersonalWorkspace.Id);

        var agent = await harness.AgentDashboard.CreateAsync(
            new CreateDashboardAgentRequest(
                "Org Agent",
                "openai",
                "gpt-4o-mini",
                null,
                null,
                null,
                ["org-docs"],
                null,
                null,
                null),
            memberId,
            opsWorkspace.Id);

        var assignedIntegrations = await new AgentIntegrationRepository(db).ListIntegrationNamesForAgentAsync(agent.Id, CancellationToken.None);
        var orgAgents = await harness.Agents.ListAsync(new AgentFilter { WorkspaceId = opsWorkspace.Id });

        Assert.Equal(MemberStatus.Active, member.Status);
        Assert.Equal(WorkspaceRole.Editor, memberOrgDefault.Role);
        Assert.Equal(WorkspaceRole.Viewer, opsMembership.Role);
        Assert.Equal(WorkspaceRole.Editor, upgradedMembership.Role);
        Assert.NotNull(memberVisibleIntegration);
        Assert.True(memberVisibleIntegration.CredentialConfigured);
        Assert.Null(personalIntegration);
        Assert.Contains("org-docs", assignedIntegrations);
        Assert.Single(orgAgents);
        Assert.Equal(memberId, orgAgents[0].OwnerId);
    }

    [Fact]
    public async Task Access_groups_grant_workspace_access_without_direct_workspace_membership()
    {
        await using var db = CreateDb();
        var ownerId = Guid.NewGuid();
        var memberId = Guid.NewGuid();
        await SeedUserAsync(db, ownerId, "owner@example.com");
        await SeedUserAsync(db, memberId, "member@example.com");

        var harness = CreateHarness(db);
        var overview = await harness.Organizations.GetOverviewAsync(ownerId, "owner@example.com", "Owner");
        await harness.Organizations.InviteMemberAsync(ownerId, "owner@example.com", "Owner", "member@example.com", "Member");
        var workspace = await harness.Workspaces.CreateOrganizationWorkspaceAsync(ownerId, overview.Organization.Id, "Finance");
        var group = await harness.AccessGroups.CreateAsync(ownerId, overview.Organization.Id, "Finance");

        await harness.AccessGroups.AddMemberAsync(ownerId, group.Id, memberId);
        await harness.AccessGroups.GrantWorkspaceAsync(ownerId, group.Id, workspace.Id, "Viewer");

        var workspaces = await harness.Workspaces.ListAsync(memberId);
        var accessible = Assert.Single(workspaces, item => item.Id == workspace.Id);

        Assert.Equal(WorkspaceRole.Viewer, accessible.Role);
    }

    private static Harness CreateHarness(EaosDbContext db)
    {
        var cache = new InMemoryDistributedCache();
        var organizationRepository = new OrganizationRepository(db);
        var workspaceRepository = new WorkspaceRepository(db);
        var workspaceMemberRepository = new WorkspaceMemberRepository(db);
        var accessGroupRepository = new AccessGroupRepository(db);
        var agentRepository = new AgentRepository(db);
        var channelRepository = new ChannelRepository(db);
        var integrationDefinitionRepository = new IntegrationDefinitionRepository(db);
        var integrationCredentialRepository = new IntegrationCredentialRepository(db);
        var integrationService = CreateIntegrationService(
            db,
            agentRepository,
            integrationDefinitionRepository,
            integrationCredentialRepository);
        var agentService = new AgentService(
            agentRepository,
            new FakeAgentDeployer(),
            new FakeProviderService(),
            NullLogger<AgentService>.Instance,
            cache,
            new AgentPersonalityRepository(db),
            new NoopPublisher(),
            new AgentChannelBinder(channelRepository),
            new FakeAgentLogService(),
            integrationService,
            new AgentToolPermissionRepository(db));
        var agentDashboard = new AgentDashboardService(
            agentService,
            agentRepository,
            new AgentSessionRepository(db),
            new AgentResourceRepository(db),
            new MemoryStoreRepository(db),
            channelRepository,
            new FakeChannelService(),
            new FakeBrowserService(),
            new AgentToolPermissionRepository(db),
            new AgentRunRepository(db));

        return new Harness(
            new WorkspaceService(workspaceRepository, workspaceMemberRepository, organizationRepository, cache),
            new OrganizationService(organizationRepository, workspaceRepository, workspaceMemberRepository),
            new AccessGroupService(accessGroupRepository, organizationRepository, workspaceRepository),
            integrationService,
            agentDashboard,
            agentRepository);
    }

    private static IntegrationDefinitionService CreateIntegrationService(
        EaosDbContext db,
        IAgentRepository agentRepository,
        IIntegrationDefinitionRepository integrationDefinitionRepository,
        IIntegrationCredentialRepository integrationCredentialRepository)
    {
        var keyRingPath = Path.Combine(Path.GetTempPath(), $"eaos-e2e-keys-{Guid.NewGuid():N}");
        var protector = new CredentialProtector(DataProtectionProvider.Create(new DirectoryInfo(keyRingPath)));
        return new IntegrationDefinitionService(
            new AgentIntegrationRepository(db),
            agentRepository,
            integrationDefinitionRepository,
            integrationCredentialRepository,
            new OAuthTokenRepository(db),
            protector,
            new GoogleOAuthConfig(),
            NullLogger<IntegrationDefinitionService>.Instance,
            new IntegrationDeploymentRepository(db),
            new WorkspaceRepository(db));
    }

    private static IntegrationDefinitionRecord CustomIntegration() => new()
    {
        Name = "org-docs",
        Title = "Org Docs",
        TransportType = IntegrationTransportType.Stdio,
        Command = "npx",
        Args = """["-y","org-docs"]""",
        Category = "custom",
        CredentialFieldsJson = """[{"name":"API_KEY","label":"API Key","type":"password","required":true}]""",
    };

    private static async Task SeedUserAsync(EaosDbContext db, Guid userId, string email)
    {
        db.Users.Add(new UserEntity
        {
            Id = userId,
            Email = email,
            Name = email.Split('@')[0],
            CreatedAt = DateTime.UtcNow,
            LastLoginAt = DateTime.UtcNow,
        });
        await db.SaveChangesAsync();
    }

    private static EaosDbContext CreateDb()
    {
        var options = new DbContextOptionsBuilder<EaosDbContext>()
            .UseInMemoryDatabase($"workspace-e2e-{Guid.NewGuid():N}")
            .Options;
        return new EaosDbContext(options);
    }

    private sealed record Harness(
        IWorkspaceService Workspaces,
        IOrganizationService Organizations,
        IAccessGroupService AccessGroups,
        IIntegrationDefinitionService Integrations,
        IAgentDashboardService AgentDashboard,
        IAgentRepository Agents);

    private sealed class InMemoryDistributedCache : IDistributedCache
    {
        private readonly Dictionary<string, byte[]> _values = new();

        public byte[]? Get(string key) => _values.GetValueOrDefault(key);
        public Task<byte[]?> GetAsync(string key, CancellationToken token = default) => Task.FromResult(Get(key));
        public void Refresh(string key) { }
        public Task RefreshAsync(string key, CancellationToken token = default) => Task.CompletedTask;
        public void Remove(string key) => _values.Remove(key);
        public Task RemoveAsync(string key, CancellationToken token = default) { Remove(key); return Task.CompletedTask; }
        public void Set(string key, byte[] value, DistributedCacheEntryOptions options) => _values[key] = value;
        public Task SetAsync(string key, byte[] value, DistributedCacheEntryOptions options, CancellationToken token = default)
        {
            Set(key, value, options);
            return Task.CompletedTask;
        }
    }

    private sealed class FakeProviderService : IProviderService
    {
        public Task<IReadOnlyList<ProviderResult>> ListAsync(CancellationToken ct = default) =>
            Task.FromResult<IReadOnlyList<ProviderResult>>([]);

        public Task<IReadOnlyList<ProviderResult>> ListForWorkspaceAsync(Guid? workspaceId, CancellationToken ct = default) =>
            ListAsync(ct);

        public Task<string?> GetApiKeyForDispatchAsync(string name, CancellationToken ct = default) =>
            Task.FromResult<string?>("test-key");

        public Task<string?> GetApiKeyForDispatchAsync(string name, Guid? workspaceId, CancellationToken ct = default) =>
            GetApiKeyForDispatchAsync(name, ct);

        public Task<bool> IsModelAllowedAsync(string provider, string? model, Guid? workspaceId, CancellationToken ct = default) =>
            Task.FromResult(true);
    }

    private sealed class FakeAgentDeployer : IAgentDeployer
    {
        public Task<AgentDeployment> DeployAsync(Guid agentId, CancellationToken ct = default) =>
            Task.FromResult(new AgentDeployment($"agent-{agentId:N}", "http://agent"));

        public Task<bool> RemoveAsync(string podName, CancellationToken ct = default) => Task.FromResult(true);
        public Task<string> GetStatusAsync(string podName, CancellationToken ct = default) => Task.FromResult("running");
        public Task<string> GetLogsAsync(string podName, int tailLines = 200, CancellationToken ct = default) => Task.FromResult(string.Empty);
    }

    private sealed class NoopPublisher : IPublisher
    {
        public Task Publish(object notification, CancellationToken cancellationToken = default) => Task.CompletedTask;
        public Task Publish<TNotification>(TNotification notification, CancellationToken cancellationToken = default)
            where TNotification : INotification => Task.CompletedTask;
    }

    private sealed class FakeAgentLogService : IAgentLogService
    {
        public IQueryable<AgentLogProjection> AgentLogs(Guid agentId, Guid? workspaceId = null) => Enumerable.Empty<AgentLogProjection>().AsQueryable();
        public IQueryable<AgentLogProjection> ChannelLogs(Guid channelConnectionId, Guid? workspaceId = null) => Enumerable.Empty<AgentLogProjection>().AsQueryable();
        public IQueryable<AgentLogProjection> GlobalLogs(GlobalLogFiltersRequest filters, Guid? workspaceId = null) => Enumerable.Empty<AgentLogProjection>().AsQueryable();
        public IQueryable<AuditEntry> AuditLog(Guid agentId, Guid? workspaceId = null) => Enumerable.Empty<AuditEntry>().AsQueryable();
        public Task<List<AgentLogRecord>> ListForAgentAsync(Guid agentId, DateTime? before, int limit, CancellationToken ct = default) => Task.FromResult(new List<AgentLogRecord>());
        public Task<List<AgentLogRecord>> ListForChannelConnectionAsync(Guid channelConnectionId, DateTime? before, int limit, CancellationToken ct = default) => Task.FromResult(new List<AgentLogRecord>());
        public Task<GlobalLogsPage> ListGlobalAsync(GlobalLogFiltersRequest filters, CancellationToken ct = default) => Task.FromResult(new GlobalLogsPage([], 0));
        public Task<AgentLogRecord> AppendAsync(AgentLogRecord record, CancellationToken ct = default) => Task.FromResult(record);
        public Task<AgentLogRecord> SendMessageAsync(Guid agentId, string content, Guid userId, CancellationToken ct = default) => Task.FromResult(AgentLogRecord.MessageIn(agentId, content));
        public Task RecordToolCallAsync(Guid agentId, Guid? userId, string skillName, string action, string paramsJson, string? resultSummary, long durationMs, CancellationToken ct = default) => Task.CompletedTask;
        public Task<(List<AgentLogRecord> Items, int Total)> GetAuditLogAsync(Guid agentId, int limit, int offset, CancellationToken ct = default) => Task.FromResult((new List<AgentLogRecord>(), 0));
        public Task<Dictionary<string, AgentLogRecord>> GetResultsByCorrelationAsync(Guid agentId, IReadOnlyCollection<string> correlationIds, CancellationToken ct = default) => Task.FromResult(new Dictionary<string, AgentLogRecord>());
    }

    private sealed class FakeBrowserService : IBrowserService
    {
        public Task<BrowserSessionState> GetOrCreateAsync(Guid agentId, CancellationToken ct = default) => throw new NotSupportedException();
        public Task<BrowserSessionState?> GetStateAsync(Guid agentId, CancellationToken ct = default) => Task.FromResult<BrowserSessionState?>(null);
        public Task<BrowserSessionState> RestartAsync(Guid agentId, CancellationToken ct = default) => throw new NotSupportedException();
        public Task StopAsync(Guid agentId, CancellationToken ct = default) => Task.CompletedTask;
        public Task<string?> GetViewUrlAsync(Guid agentId, CancellationToken ct = default) => Task.FromResult<string?>(null);
    }

    private sealed class FakeChannelService : IChannelService
    {
        public Task<IReadOnlyList<Guid>> RouteInboundAsync(Guid connectionId, string senderIdentifier, string messageText, bool isGroupMessage, string? messageId, string? channelId, CancellationToken ct = default) => Task.FromResult<IReadOnlyList<Guid>>([]);
        public Task<IReadOnlyList<Guid>> RouteInboundByChannelTypeAsync(string channelType, string senderIdentifier, string messageText, bool isGroupMessage, string? messageId, string? channelId, CancellationToken ct = default) => Task.FromResult<IReadOnlyList<Guid>>([]);
        public Task BroadcastAsync(Guid agentId, string text, CancellationToken ct = default) => Task.CompletedTask;
        public Task SendTestMessageAsync(Guid connectionId, CancellationToken ct = default) => Task.CompletedTask;
        public Task<ChannelConnectionRecord> CreateConnectionAsync(string channelType, string displayName, string? configJson, Guid createdById, Guid workspaceId, CancellationToken ct = default) => throw new NotSupportedException();
        public Task<ChannelConnectionRecord> UpdateConnectionAsync(Guid id, string? displayName, bool? enabled, CancellationToken ct = default) => throw new NotSupportedException();
        public Task<ChannelConnectionRecord> UpdateOwnedConnectionAsync(Guid id, Guid ownerId, Guid workspaceId, string? displayName, bool? enabled, CancellationToken ct = default) => throw new NotSupportedException();
        public Task<bool> DeleteConnectionAsync(Guid id, CancellationToken ct = default) => Task.FromResult(false);
        public Task<bool> DeleteOwnedConnectionAsync(Guid id, Guid ownerId, Guid workspaceId, CancellationToken ct = default) => Task.FromResult(false);
        public Task SaveChannelCredsAsync(Guid connectionId, string credsJson, CancellationToken ct = default) => Task.CompletedTask;
        public Task<IReadOnlyList<AgentChannelBindingRecord>> ListBindingsForOwnedAgentAsync(Guid agentId, Guid ownerId, Guid? workspaceId = null, CancellationToken ct = default) => Task.FromResult<IReadOnlyList<AgentChannelBindingRecord>>([]);
        public Task<AgentChannelBindingRecord> BindAgentAsync(Guid agentId, Guid channelConnectionId, string? configJson, CancellationToken ct = default) => throw new NotSupportedException();
        public Task<bool> UnbindAgentAsync(Guid agentId, Guid channelConnectionId, CancellationToken ct = default) => Task.FromResult(false);
        public Task<AgentChannelBindingRecord> UpdateBindingConfigAsync(Guid agentId, Guid channelConnectionId, string configJson, CancellationToken ct = default) => throw new NotSupportedException();
    }
}
