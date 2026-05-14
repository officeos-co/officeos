using OffceOs.Application.Features.AgentDefinitions;
using OffceOs.Application.Features.Agents;
using OffceOs.Application.Features.Browser;
using OffceOs.Application.Features.AgentRoutines;
using OffceOs.Application.Features.Channels;
using OffceOs.Application.Features.Integrations;
using OffceOs.Application.Features.Management;
using OffceOs.Application.Features.Providers;
using OffceOs.Database;
using OffceOs.Database.Models;
using OffceOs.Domain.Features.Agents;
using OffceOs.Domain.Features.Browser;
using OffceOs.Domain.Features.Channels;
using OffceOs.Domain.Features.Integrations;
using OffceOs.Domain.Features.Management;
using OffceOs.Domain.Features.Providers;
using OffceOs.Infrastructure.Common.Security;
using OffceOs.Infrastructure.Features.Agents;
using OffceOs.Infrastructure.Features.AgentRoutines;
using OffceOs.Infrastructure.Features.Browser;
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
            NullLogger<AgentService>.Instance,
            cache,
            new AgentPersonalityRepository(db),
            new NoopPublisher(),
            new AgentChannelBinder(channelRepository),
            new FakeAgentLogService(),
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
                NullLogger<ChannelService>.Instance),
            new FakeBrowserService(),
            new FakeAgentDeployer(),
            new AgentRunRepository(db),
            new FakeAgentLogService(),
            agentDefinitionParser,
            new AgentRoutineService(
                new AgentRoutineRepository(db),
                agentRepository,
                new CredentialProtector(DataProtectionProvider.Create(new DirectoryInfo(Path.Combine(Path.GetTempPath(), $"eaos-routine-e2e-keys-{Guid.NewGuid():N}"))))));

        return new WorkspaceTestHarness(
            new WorkspaceService(workspaceRepository, workspaceMemberRepository, new FakeAgentLogService(), cache),
            new IntegrationDeploymentService(integrationDeploymentRepository, workspaceMemberRepository, new FakeAgentLogService()),
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
            NullLogger<IntegrationDefinitionService>.Instance,
            new IntegrationDeploymentRepository(db),
            new WorkspaceRepository(db),
            new WorkspaceMemberRepository(db));
    }
}
