using OffceOs.Features.AgentDefinitions.Application;
using OffceOs.Features.Agents.Application;
using OffceOs.Features.AgentRoutines.Application;
using OffceOs.Features.Channels.Application;
using OffceOs.Features.Integrations.Application;
using OffceOs.Features.Management.Application;
using OffceOs.Database;
using OffceOs.Database.Models;
using OffceOs.Features.Agents.Domain;
using OffceOs.Features.Channels.Domain;
using OffceOs.Features.Integrations.Domain;
using OffceOs.Features.Management.Domain;
using OffceOs.Common.Infrastructure.Security;
using OffceOs.Features.Agents.Infrastructure;
using OffceOs.Features.AgentRoutines.Infrastructure;
using OffceOs.Features.Browser.Infrastructure;
using OffceOs.Features.Channels.Infrastructure;
using OffceOs.Features.Context.Infrastructure;
using OffceOs.Features.Integrations.Infrastructure;
using OffceOs.Features.Management.Infrastructure;

namespace OffceOs.Tests.Shared;

public sealed record WorkspaceTestHarness(
    IWorkspaceService Workspaces,
    IIntegrationDeploymentService IntegrationDeployments,
    IIntegrationDefinitionService Integrations,
    IAgentLifecycleService AgentLifecycle,
    IAgentRepository Agents)
{
    public static WorkspaceTestHarness Create(EaosDbContext db)
    {
        var cache = new InMemoryDistributedCache();
        var workspaceRepository = new WorkspaceRepository(db);
        var workspaceMemberRepository = new WorkspaceMemberRepository(db);
        var integrationDeploymentRepository = new IntegrationDeploymentRepository(db);
        var agentRepository = new AgentRepository(db);
        var channelRepository = new ChannelRepository(db);
        var integrationDefinitionRepository = new IntegrationDefinitionRepository(db);
        var integrationCredentialRepository = new IntegrationCredentialRepository(db);
        var agentDefinitionRepository = new AgentDefinitionRepository(db);
        var agentDefinitionParser = new AgentDefinitionParser();
        var integrationService = CreateIntegrationService(
            db,
            agentRepository,
            integrationDefinitionRepository,
            integrationCredentialRepository);
        var agentService = new AgentService(
            agentRepository,
            new FakeAgentDeployer(),
            new FakeProviderService(),
            cache,
            new AgentPersonalityRepository(db),
            new NoopPublisher(),
            new AgentChannelBinder(channelRepository),
            new FakeResourceLogService(),
            new FakeResourceLogWriterService(),
            integrationService,
            agentDefinitionRepository,
            agentDefinitionParser);
        var agentResource = new AgentLifecycleService(
            agentService,
            agentRepository,
            new AgentSessionRepository(db),
            new AgentResourceRepository(db),
            new BrowserResourceRepository(db),
            new MemoryStoreRepository(db),
            channelRepository,
            new ChannelService(
                channelRepository,
                new RecordingChannelGateway(),
                agentRepository,
                new ChannelCredentialProtector(DataProtectionProvider.Create(new DirectoryInfo(Path.Combine(Path.GetTempPath(), $"eaos-channel-e2e-keys-{Guid.NewGuid():N}")))),
                new NoopPublisher(),
                new ChannelReplyContext(),
                new FakeResourceLogWriterService()),
            new FakeBrowserService(),
            new FakeAgentDeployer(),
            new FakeResourceLogService(),
            agentDefinitionRepository,
            agentDefinitionParser,
            new AgentRoutineService(
                new AgentRoutineRepository(db),
                agentRepository,
                new CredentialProtector(DataProtectionProvider.Create(new DirectoryInfo(Path.Combine(Path.GetTempPath(), $"eaos-routine-e2e-keys-{Guid.NewGuid():N}"))))));

        return new WorkspaceTestHarness(
            new WorkspaceService(workspaceRepository, workspaceMemberRepository, new FakeResourceLogService(), cache),
            new IntegrationDeploymentService(integrationDeploymentRepository, workspaceMemberRepository, new FakeResourceLogService()),
            integrationService,
            agentResource,
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
        return new IntegrationDefinitionService(
            new AgentIntegrationRepository(db),
            agentRepository,
            integrationDefinitionRepository,
            integrationCredentialRepository,
            new FakeIntegrationCredentialEncryptionService(),
            new FakeResourceLogWriterService(),
            new IntegrationDeploymentRepository(db),
            new WorkspaceRepository(db),
            new WorkspaceMemberRepository(db));
    }
}
