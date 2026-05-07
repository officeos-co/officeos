using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using EnterpriseAgentOs.Infrastructure.Features.Agents.Adapters;

namespace EnterpriseAgentOs.Infrastructure;

public static class InfrastructureServiceRegistration
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, string connectionString)
    {
        // DbContext
        services.AddDbContext<EaosDbContext>(options =>
            options.UseNpgsql(connectionString));

        // Repositories
        services.AddScoped<IAgentRepository, AgentRepository>();
        services.AddScoped<IAgentToolPermissionRepository, AgentToolPermissionRepository>();

        services.AddScoped<IBrowserSessionRepository, BrowserSessionRepository>();
        services.AddScoped<IMcpServerRepository, McpServerRepository>();
        services.AddScoped<IAgentMcpServerRepository, AgentMcpServerRepository>();
        services.AddScoped<IMcpCredentialRepository, McpCredentialRepository>();
        services.AddScoped<IAtlasConnectionRepository, AtlasConnectionRepository>();
        services.AddScoped<IAtlasEntityStatusRepository, AtlasEntityStatusRepository>();
        services.AddScoped<IAtlasIndexJobRepository, AtlasIndexJobRepository>();
        services.AddScoped<IAtlasIndexedRecordRepository, AtlasIndexedRecordRepository>();
        services.AddScoped<IAtlasActivityRepository, AtlasActivityRepository>();
        services.AddScoped<IAtlasRequestHistoryRepository, AtlasRequestHistoryRepository>();
        services.AddScoped<IOAuthTokenRepository, OAuthTokenRepository>();
        services.AddScoped<IUserRepository, UserRepository>();
        services.AddScoped<ISessionRepository, SessionRepository>();
        services.AddScoped<IChannelRepository, ChannelRepository>();
        services.AddScoped<IAgentLogRepository, AgentLogRepository>();
        services.AddScoped<IOrganizationRepository, OrganizationRepository>();
        services.AddScoped<IUserSubscriptionRepository, UserSubscriptionRepository>();
        services.AddScoped<IOrgSubscriptionRepository, OrgSubscriptionRepository>();
        services.AddScoped<IAgentMemoryRepository, AgentMemoryRepository>();
        services.AddScoped<IAgentPersonalityRepository, AgentPersonalityRepository>();
        services.AddScoped<IAgentCronJobRepository, AgentCronJobRepository>();
        services.AddScoped<IAgentSessionRepository, AgentSessionRepository>();
        services.AddScoped<IAgentSessionContextRepository, AgentSessionContextRepository>();
        services.AddScoped<IAgentRunRepository, AgentRunRepository>();

        // Adapters
        services.AddScoped<IChannelGateway, ChannelSidecarGateway>();
        services.AddScoped<LlmProviderDispatcher>();
        services.AddScoped<IStripeWebhookService, StripeWebhookService>();
        services.AddScoped<IStripeMeteringService, StripeMeteringService>();
        // Adapters — MCP
        services.AddSingleton<IMcpClientManager, McpClientManager>();

        // Protectors
        services.AddSingleton<CredentialProtector>();
        services.AddSingleton<ChannelCredentialProtector>();

        // HTTP clients
        services.AddHttpClient<IPostHogService, PostHogService>();
        services.AddHttpClient("agent-proxy");
        services.AddHttpClient("llm-proxy");
        services.AddHttpClient<IBrowserRuntimeClient, AutoBrowserRuntimeClient>();
        services.AddHttpClient("github-atlas", client =>
        {
            client.BaseAddress = new Uri("https://api.github.com/");
            client.DefaultRequestHeaders.UserAgent.ParseAdd("OfficeOS-Atlas/1.0");
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
