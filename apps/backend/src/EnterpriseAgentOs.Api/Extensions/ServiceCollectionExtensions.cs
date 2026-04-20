namespace EnterpriseAgentOs.Api.Extensions;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddRepositories(this IServiceCollection services)
    {
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
        return services;
    }

    public static IServiceCollection AddApplicationServices(this IServiceCollection services)
    {
        services.AddScoped<IAgentService, Application.Services.Agents.AgentService>();
        services.AddScoped<IProviderService, Application.Services.Providers.ProviderService>();
        services.AddScoped<ISkillService, Application.Services.Skills.SkillService>();
        services.AddScoped<IWorkOsAuthService, WorkOsAuthService>();
        services.AddScoped<ChannelMessageRouter>();
        services.AddSingleton<IChannelAdapter, Infrastructure.Adapters.Channels.Adapters.SlackAdapter>();
        services.AddSingleton<IChannelAdapter, Infrastructure.Adapters.Channels.Adapters.TelegramAdapter>();
        services.AddSingleton<IChannelAdapter, Infrastructure.Adapters.Channels.Adapters.DiscordAdapter>();
        services.AddSingleton<IChannelAdapter, Infrastructure.Adapters.Channels.Adapters.WhatsAppAdapter>();
        services.AddSingleton<IChannelAdapter, Infrastructure.Adapters.Channels.Adapters.TeamsAdapter>();
        services.AddSingleton<IChannelAdapter, Infrastructure.Adapters.Channels.Adapters.GoogleChatAdapter>();
        services.AddSingleton<ChannelAdapterRegistry>();
        services.AddScoped<LlmProviderDispatcher>();
        services.AddScoped<IUserBillingService, Application.Services.Billing.UserBillingService>();
        services.AddScoped<IOrgBillingService, Application.Services.Billing.OrgBillingService>();
        services.AddScoped<IStripeWebhookService, StripeWebhookService>();
        services.AddScoped<ICreditRecordingService, Application.Services.Billing.CreditRecordingService>();
        services.AddScoped<IGdprService, Application.Services.Auth.GdprService>();
        services.AddScoped<IAgentTemplateService, Application.Services.AgentTemplates.AgentTemplateService>();
        services.AddScoped<IAgentLogService, Application.Services.AgentLogs.AgentLogService>();
        services.AddScoped<Application.Services.Agents.PromptComposer>();
        services.AddScoped<Application.Services.Agents.AgentTurnService>();
        return services;
    }

    public static IServiceCollection AddBackgroundServices(this IServiceCollection services)
    {
        services.AddHostedService<Application.Services.CronJobs.CronJobSchedulerService>();
        return services;
    }

    public static IServiceCollection AddProtectors(this IServiceCollection services)
    {
        services.AddSingleton<ProviderKeyProtector>();
        services.AddSingleton<SkillCredentialProtector>();
        services.AddSingleton<ChannelConfigProtector>();
        return services;
    }

    public static IServiceCollection AddHttpClients(this IServiceCollection services)
    {
        services.AddHttpClient<SkillRuntimeClient>();
        // PostHog is HttpClient-first — registering it here gives us a typed
        // HttpClient and also registers IPostHogService -> PostHogService in
        // DI, so no separate AddScoped call is required.
        services.AddHttpClient<IPostHogService, PostHogService>();
        services.AddHttpClient("agent-proxy");
        services.AddHttpClient("llm-proxy");
        services.AddHttpClient("channel-platform");
        return services;
    }
}
