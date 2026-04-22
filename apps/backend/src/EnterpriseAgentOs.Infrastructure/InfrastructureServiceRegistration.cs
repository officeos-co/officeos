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
        services.AddScoped<IProviderRepository, ProviderRepository>();
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
        services.AddScoped<ChannelMessageRouter>();
        services.AddScoped<IChannelGateway, ChannelGateway>();
        services.AddSingleton<IChannelAdapter, SlackAdapter>();
        services.AddSingleton<IChannelAdapter, TelegramAdapter>();
        services.AddSingleton<IChannelAdapter, DiscordAdapter>();
        services.AddSingleton<IChannelAdapter, TeamsAdapter>();
        services.AddSingleton<IChannelAdapter, GoogleChatAdapter>();
        services.AddSingleton<ChannelAdapterRegistry>();
        services.AddScoped<LlmProviderDispatcher>();
        services.AddScoped<IStripeWebhookService, StripeWebhookService>();

        // Protectors
        services.AddSingleton<ProviderKeyProtector>();
        services.AddSingleton<SkillCredentialProtector>();
        services.AddSingleton<ChannelConfigProtector>();
        services.AddSingleton<Channels.Common.InMemoryMessageDeduplicator>();

        // HTTP clients
        services.AddHttpClient<SkillRuntimeClient>();
        services.AddHttpClient<IPostHogService, PostHogService>();
        services.AddHttpClient("agent-proxy");
        services.AddHttpClient("llm-proxy");
        services.AddHttpClient("channel-platform");
        services.AddHttpClient("whatsapp-gateway");

        // WhatsApp gateway (HTTP proxy to Baileys sidecar)
        services.AddSingleton<WhatsAppGatewayService>();

        return services;
    }
}
