using OffceOs.Application.Features.Agents;
using OffceOs.Application.Features.Channels;
using OffceOs.Application.Features.Integrations;
using OffceOs.Application.Features.Management;
using OffceOs.Application.Features.Providers;
using OffceOs.Configuration;
using OffceOs.Database;
using OffceOs.Database.Models;
using OffceOs.Domain.Features.Agents;
using OffceOs.Domain.Features.Integrations;
using OffceOs.Domain.Features.Management;
using OffceOs.Domain.Features.Providers;
using OffceOs.Infrastructure.Common.Security;
using OffceOs.Infrastructure.Features.Agents;
using OffceOs.Infrastructure.Features.Billing;
using OffceOs.Infrastructure.Features.Channels;
using OffceOs.Infrastructure.Features.Context;
using OffceOs.Infrastructure.Features.Integrations;
using OffceOs.Infrastructure.Features.Management;
using OffceOs.Infrastructure.Features.Providers;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;

namespace OffceOs.Tests.Shared;

public sealed record WorkspaceTestHarness(
    IWorkspaceService Workspaces,
    IOrganizationService Organizations,
    IAccessGroupService AccessGroups,
    IOrganizationPolicyService Policy,
    IOrganizationProviderProfileService ProviderProfiles,
    IIntegrationDeploymentService IntegrationDeployments,
    IIntegrationDefinitionService Integrations,
    IAgentDashboardService AgentDashboard,
    IAgentRepository Agents)
{
    public static WorkspaceTestHarness Create(EaosDbContext db)
    {
        var cache = new InMemoryDistributedCache();
        var organizationRepository = new OrganizationRepository(db);
        var workspaceRepository = new WorkspaceRepository(db);
        var workspaceMemberRepository = new WorkspaceMemberRepository(db);
        var accessGroupRepository = new AccessGroupRepository(db);
        var organizationPolicyProfileRepository = new OrganizationPolicyProfileRepository(db);
        var organizationProviderProfileRepository = new OrganizationProviderProfileRepository(db);
        var orgSubscriptionRepository = new OrgSubscriptionRepository(db);
        var integrationDeploymentRepository = new IntegrationDeploymentRepository(db);
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

        return new WorkspaceTestHarness(
            new WorkspaceService(workspaceRepository, workspaceMemberRepository, organizationRepository, cache, new NoopPublisher()),
            new OrganizationService(organizationRepository, workspaceRepository, workspaceMemberRepository, new NoopPublisher()),
            new AccessGroupService(accessGroupRepository, organizationRepository, workspaceRepository, new NoopPublisher()),
            new OrganizationPolicyService(organizationPolicyProfileRepository, organizationRepository, workspaceRepository, new NoopPublisher()),
            new OrganizationProviderProfileService(
                organizationProviderProfileRepository,
                organizationRepository,
                new CredentialProtector(DataProtectionProvider.Create(new DirectoryInfo(Path.Combine(Path.GetTempPath(), $"eaos-provider-e2e-keys-{Guid.NewGuid():N}")))),
                new ProviderEnterprisePolicy(orgSubscriptionRepository),
                new NoopPublisher()),
            new IntegrationDeploymentService(integrationDeploymentRepository, organizationRepository, workspaceRepository),
            integrationService,
            agentDashboard,
            agentRepository);
    }

    public static IntegrationDefinitionRecord CustomIntegration() => new()
    {
        Name = "org-docs",
        Title = "Org Docs",
        TransportType = IntegrationTransportType.Stdio,
        Command = "npx",
        Args = """["-y","org-docs"]""",
        Category = "custom",
        CredentialFieldsJson = """[{"name":"API_KEY","label":"API Key","type":"password","required":true}]""",
    };

    public static async Task SeedUserAsync(EaosDbContext db, Guid userId, string email)
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

    public static EaosDbContext CreateDb(string namePrefix = "workspace-e2e")
    {
        var options = new DbContextOptionsBuilder<EaosDbContext>()
            .UseInMemoryDatabase($"{namePrefix}-{Guid.NewGuid():N}")
            .Options;
        return new EaosDbContext(options);
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
            new WorkspaceRepository(db),
            new OrganizationRepository(db));
    }
}
