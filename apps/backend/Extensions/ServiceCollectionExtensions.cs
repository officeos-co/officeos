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
        return services;
    }

    public static IServiceCollection AddApplicationServices(this IServiceCollection services)
    {
        services.AddScoped<IAgentService, EnterpriseAgentOs.Application.Services.Agents.AgentService>();
        services.AddScoped<IProviderService, EnterpriseAgentOs.Application.Services.Providers.ProviderService>();
        services.AddScoped<ISkillService, EnterpriseAgentOs.Application.Services.Skills.SkillService>();
        services.AddScoped<IWorkOsAuthService, WorkOsAuthService>();
        services.AddScoped<ChannelMessageRouter>();
        services.AddScoped<LlmProviderDispatcher>();
        services.AddScoped<IUserBillingService, EnterpriseAgentOs.Application.Services.Billing.UserBillingService>();
        services.AddScoped<IOrgBillingService, EnterpriseAgentOs.Application.Services.Billing.OrgBillingService>();
        services.AddScoped<IStripeWebhookService, StripeWebhookService>();
        services.AddScoped<ICreditRecordingService, EnterpriseAgentOs.Application.Services.Billing.CreditRecordingService>();
        services.AddScoped<IGdprService, EnterpriseAgentOs.Application.Services.Gdpr.GdprService>();
        services.AddScoped<IAgentTemplateService, EnterpriseAgentOs.Application.Services.AgentTemplates.AgentTemplateService>();
        services.AddScoped<IAgentLogService, EnterpriseAgentOs.Application.Services.AgentLogs.AgentLogService>();
        return services;
    }

    public static IServiceCollection AddBackgroundServices(this IServiceCollection services)
    {
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
