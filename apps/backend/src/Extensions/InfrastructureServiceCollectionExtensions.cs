using OffceOs.Database;
using OffceOs.Features.AgentRoutines.Domain;
using OffceOs.Features.Agents.Domain;
using OffceOs.Features.Browser.Domain;
using OffceOs.Features.Channels.Domain;
using OffceOs.Features.Context.Domain;
using OffceOs.Features.Integrations.Domain;
using OffceOs.Features.Management.Domain;
using OffceOs.Features.Providers.Domain;
using OffceOs.Features.ResourceLogs.Domain;
using OffceOs.Common.Infrastructure.Security;
using OffceOs.Features.AgentRoutines.Infrastructure;
using OffceOs.Features.AgentHarness.Application;
using OffceOs.Features.AgentHarness.Infrastructure;
using OffceOs.Features.Agents.Infrastructure;
using OffceOs.Features.Browser.Infrastructure;
using OffceOs.Features.Channels.Infrastructure;
using OffceOs.Features.Context.Infrastructure;
using OffceOs.Features.Integrations.Infrastructure;
using OffceOs.Features.Management.Infrastructure;
using OffceOs.Features.Providers.Infrastructure;
using OffceOs.Features.ResourceLogs.Infrastructure;
namespace OffceOs.Extensions;

public static class InfrastructureServiceCollectionExtensions
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, string connectionString)
    {
        // DbContext
        services.AddDbContext<EaosDbContext>(options =>
            options.UseNpgsql(connectionString));

        // Repositories
        services.AddScoped<IAgentRepository, AgentRepository>();
        services.AddScoped<IAgentDefinitionRepository, AgentDefinitionRepository>();

        services.AddScoped<IBrowserResourceRepository, BrowserResourceRepository>();
        services.AddScoped<IBrowserSessionRepository, BrowserSessionRepository>();
        services.AddScoped<IIntegrationDefinitionRepository, IntegrationDefinitionRepository>();
        services.AddScoped<IAgentIntegrationRepository, AgentIntegrationRepository>();
        services.AddScoped<IIntegrationCredentialRepository, IntegrationCredentialRepository>();
        services.AddScoped<IIntegrationDeploymentRepository, IntegrationDeploymentRepository>();
        services.AddScoped<IMemoryStoreRepository, MemoryStoreRepository>();
        services.AddScoped<IUserRepository, UserRepository>();
        services.AddScoped<IDeviceCodeRepository, DeviceCodeRepository>();
        services.AddScoped<IWorkspaceRepository, WorkspaceRepository>();
        services.AddScoped<IWorkspaceMemberRepository, WorkspaceMemberRepository>();
        services.AddScoped<ISessionRepository, SessionRepository>();
        services.AddScoped<IChannelRepository, ChannelRepository>();
        services.AddScoped<IResourceLogRepository, ResourceLogRepository>();
        services.AddScoped<IProviderResourceRepository, ProviderResourceRepository>();
        services.AddScoped<IAgentMemoryRepository, AgentMemoryRepository>();
        services.AddScoped<IAgentPersonalityRepository, AgentPersonalityRepository>();
        services.AddScoped<IAgentRoutineCredentialRepository, AgentRoutineCredentialRepository>();
        services.AddScoped<IAgentRoutineRepository, AgentRoutineRepository>();
        services.AddScoped<IAgentSessionRepository, AgentSessionRepository>();
        services.AddScoped<IAgentSessionContextRepository, AgentSessionContextRepository>();
        services.AddScoped<IAgentResourceRepository, AgentResourceRepository>();
        services.AddScoped<ICodeSessionService, GitHubCodeSessionService>();

        // Adapters
        services.AddScoped<IChannelGateway, ChannelSidecarGateway>();
        services.AddScoped<LlmProviderDispatcher>();
        services.AddScoped<ICloudProviderTokenService, CloudProviderTokenService>();
        // Adapters - integrations
        services.AddSingleton<IIntegrationClientManager, IntegrationClientManager>();

        // Protectors
        services.AddSingleton<CredentialProtector>();
        services.AddSingleton<ChannelCredentialProtector>();
        services.AddScoped<IIntegrationCredentialEncryptionService, IntegrationCredentialEncryptionService>();

        // HTTP clients
        services.AddHttpClient("vault-transit");
        services.AddHttpClient("agent-proxy");
        services.AddHttpClient("llm-proxy");
        services.AddHttpClient<IBrowserRuntimeClient, AutoBrowserRuntimeClient>();
        services.AddHttpClient("github-api", client =>
        {
            client.BaseAddress = new Uri("https://api.github.com/");
            client.DefaultRequestHeaders.UserAgent.ParseAdd("OfficeOS-Integration/1.0");
            client.DefaultRequestHeaders.Accept.ParseAdd("application/vnd.github+json");
        });
        services.AddHttpClient("channel-sidecar", client =>
        {
            var channelUrl = Environment.GetEnvironmentVariable("CHANNEL_SERVICE_URL")
                ?? throw new InvalidOperationException("Missing required env var: CHANNEL_SERVICE_URL");
            client.BaseAddress = new Uri(channelUrl);
        });

        return services;
    }
}
