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
        services.AddScoped<IRunnerRepository, RunnerRepository>();
        services.AddScoped<IRunnerJobRepository, RunnerJobRepository>();
        services.AddScoped<ISkillRegistryRepository, SkillRegistryRepository>();
        services.AddScoped<IChannelRepository, ChannelRepository>();
        services.AddScoped<IAuditRepository, AuditRepository>();
        services.AddScoped<IRateLimitRepository, RateLimitRepository>();
        services.AddScoped<EnterpriseAgentOs.Api.Entities.AgentTemplates.IAgentTemplateRepository, EnterpriseAgentOs.Api.Entities.AgentTemplates.AgentTemplateRepository>();
        services.AddScoped<IAgentLogRepository, AgentLogRepository>();
        return services;
    }

    public static IServiceCollection AddApplicationServices(this IServiceCollection services)
    {
        services.AddScoped<IAgentService, AgentService>();
        services.AddScoped<IProviderService, ProviderService>();
        services.AddScoped<ISkillService, SkillService>();
        services.AddScoped<IWorkOsAuthService, WorkOsAuthService>();
        services.AddScoped<ChannelMessageRouter>();
        services.AddScoped<LlmProviderDispatcher>();
        services.AddScoped<EnterpriseAgentOs.Api.Entities.Billing.IUserBillingService, EnterpriseAgentOs.Api.Entities.Billing.UserBillingService>();
        services.AddScoped<EnterpriseAgentOs.Api.Entities.Billing.IOrgBillingService, EnterpriseAgentOs.Api.Entities.Billing.OrgBillingService>();
        services.AddScoped<EnterpriseAgentOs.Api.Entities.Billing.IStripeWebhookService, EnterpriseAgentOs.Api.Entities.Billing.StripeWebhookService>();
        services.AddScoped<EnterpriseAgentOs.Api.Entities.Billing.ICreditRecordingService, EnterpriseAgentOs.Api.Entities.Billing.CreditRecordingService>();
        services.AddScoped<EnterpriseAgentOs.Api.Entities.Events.ISystemEventService, EnterpriseAgentOs.Api.Entities.Events.SystemEventService>();
        services.AddScoped<IAuditService, AuditService>();
        services.AddScoped<IRateLimitService, RateLimitService>();
        services.AddScoped<EnterpriseAgentOs.Api.Entities.Gdpr.IGdprService, EnterpriseAgentOs.Api.Entities.Gdpr.GdprService>();
        services.AddScoped<EnterpriseAgentOs.Api.Entities.AgentTemplates.IAgentTemplateService, EnterpriseAgentOs.Api.Entities.AgentTemplates.AgentTemplateService>();
        services.AddScoped<IAgentLogService, AgentLogService>();
        return services;
    }

    public static IServiceCollection AddBackgroundServices(this IServiceCollection services)
    {
        services.AddSingleton<RunnerJobWaiter>();
        services.AddSingleton<EnterpriseAgentOs.Api.Entities.Events.SystemEventBroadcaster>();
        services.AddHostedService<RunnerJobTimeoutService>();
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
        services.AddHttpClient<IVaultClient, CouchDbVaultClient>();
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
