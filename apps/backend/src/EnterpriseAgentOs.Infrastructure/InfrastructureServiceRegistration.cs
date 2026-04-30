using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

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

        services.AddScoped<ISkillRepository, SkillRepository>();
        services.AddScoped<ISkillCatalogRepository, SkillCatalogRepository>();
        services.AddScoped<IBrowserSessionRepository, BrowserSessionRepository>();
        services.AddScoped<IAgentSkillRepository, AgentSkillRepository>();
        services.AddScoped<IUserRepository, UserRepository>();
        services.AddScoped<ISessionRepository, SessionRepository>();
        services.AddScoped<IChannelRepository, ChannelRepository>();
        services.AddScoped<IAgentTemplateRepository, AgentTemplateRepository>();
        services.AddScoped<IAgentLogRepository, AgentLogRepository>();
        services.AddScoped<IOrganizationRepository, OrganizationRepository>();
        services.AddScoped<IUserSubscriptionRepository, UserSubscriptionRepository>();
        services.AddScoped<IOrgSubscriptionRepository, OrgSubscriptionRepository>();
        services.AddScoped<IAgentMemoryRepository, AgentMemoryRepository>();
        services.AddScoped<IAgentPersonalityRepository, AgentPersonalityRepository>();
        services.AddScoped<IAgentCronJobRepository, AgentCronJobRepository>();
        services.AddScoped<IAgentSessionRepository, AgentSessionRepository>();

        // Adapters
        services.AddScoped<IChannelGateway, ChannelSidecarGateway>();
        services.AddScoped<LlmProviderDispatcher>();
        services.AddScoped<IStripeWebhookService, StripeWebhookService>();
        services.AddScoped<IBillingGuard, BillingGuard>();
        services.AddScoped<ISkillOAuthService, SkillOAuthService>();

        // Protectors

        services.AddSingleton<SkillCredentialProtector>();
        services.AddSingleton<ChannelCredentialProtector>();

        // HTTP clients
        services.AddHttpClient<SkillRuntimeClient>();
        services.AddHttpClient<IPostHogService, PostHogService>();
        services.AddHttpClient("agent-proxy");
        services.AddHttpClient("llm-proxy");
        services.AddHttpClient("channel-sidecar", client =>
        {
            var channelUrl = Environment.GetEnvironmentVariable("CHANNEL_SERVICE_URL")
                ?? throw new InvalidOperationException("Missing required env var: CHANNEL_SERVICE_URL");
            client.BaseAddress = new Uri(channelUrl);
        });

        return services;
    }
}
