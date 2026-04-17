namespace EnterpriseAgentOs.Api.Extensions;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddRepositories(this IServiceCollection services)
    {
        services.AddScoped<EnterpriseAgentOs.Api.Entities.Agents.IAgentRepository, EnterpriseAgentOs.Api.Entities.Agents.AgentRepository>();
        services.AddScoped<EnterpriseAgentOs.Api.Entities.Providers.IProviderRepository, EnterpriseAgentOs.Api.Entities.Providers.ProviderRepository>();
        services.AddScoped<EnterpriseAgentOs.Api.Entities.Skills.ISkillRepository, EnterpriseAgentOs.Api.Entities.Skills.SkillRepository>();
        services.AddScoped<EnterpriseAgentOs.Api.Entities.Skills.ISkillCatalogRepository, EnterpriseAgentOs.Api.Entities.Skills.SkillCatalogRepository>();
        services.AddScoped<EnterpriseAgentOs.Api.Entities.Skills.IBrowserSessionRepository, EnterpriseAgentOs.Api.Entities.Skills.BrowserSessionRepository>();
        services.AddScoped<EnterpriseAgentOs.Api.Entities.AgentSkills.IAgentSkillRepository, EnterpriseAgentOs.Api.Entities.AgentSkills.AgentSkillRepository>();
        services.AddScoped<EnterpriseAgentOs.Api.Entities.Auth.IUserRepository, EnterpriseAgentOs.Api.Entities.Auth.UserRepository>();
        services.AddScoped<EnterpriseAgentOs.Api.Entities.Auth.ISessionRepository, EnterpriseAgentOs.Api.Entities.Auth.SessionRepository>();
        services.AddScoped<EnterpriseAgentOs.Api.Entities.Channels.IChannelRepository, EnterpriseAgentOs.Api.Entities.Channels.ChannelRepository>();
        services.AddScoped<EnterpriseAgentOs.Api.Entities.AgentTemplates.IAgentTemplateRepository, EnterpriseAgentOs.Api.Entities.AgentTemplates.AgentTemplateRepository>();
        services.AddScoped<EnterpriseAgentOs.Api.Entities.AgentLogs.IAgentLogRepository, EnterpriseAgentOs.Api.Entities.AgentLogs.AgentLogRepository>();
        services.AddScoped<EnterpriseAgentOs.Api.Entities.Organizations.IOrganizationRepository, EnterpriseAgentOs.Api.Entities.Organizations.OrganizationRepository>();
        return services;
    }

    public static IServiceCollection AddApplicationServices(this IServiceCollection services)
    {
        services.AddScoped<EnterpriseAgentOs.Api.Entities.Agents.IAgentService, EnterpriseAgentOs.Api.Entities.Agents.AgentService>();
        services.AddScoped<EnterpriseAgentOs.Api.Entities.Providers.IProviderService, EnterpriseAgentOs.Api.Entities.Providers.ProviderService>();
        services.AddScoped<EnterpriseAgentOs.Api.Entities.Skills.ISkillService, EnterpriseAgentOs.Api.Entities.Skills.SkillService>();
        services.AddScoped<EnterpriseAgentOs.Api.Entities.Sso.IWorkOsAuthService, EnterpriseAgentOs.Api.Entities.Sso.WorkOsAuthService>();
        services.AddScoped<EnterpriseAgentOs.Api.Entities.Channels.ChannelMessageRouter>();
        services.AddScoped<EnterpriseAgentOs.Api.Entities.LlmProxy.LlmProviderDispatcher>();
        services.AddScoped<EnterpriseAgentOs.Api.Entities.Billing.IUserBillingService, EnterpriseAgentOs.Api.Entities.Billing.UserBillingService>();
        services.AddScoped<EnterpriseAgentOs.Api.Entities.Billing.IOrgBillingService, EnterpriseAgentOs.Api.Entities.Billing.OrgBillingService>();
        services.AddScoped<EnterpriseAgentOs.Api.Entities.Billing.IStripeWebhookService, EnterpriseAgentOs.Api.Entities.Billing.StripeWebhookService>();
        services.AddScoped<EnterpriseAgentOs.Api.Entities.Billing.ICreditRecordingService, EnterpriseAgentOs.Api.Entities.Billing.CreditRecordingService>();
        services.AddScoped<EnterpriseAgentOs.Api.Entities.Gdpr.IGdprService, EnterpriseAgentOs.Api.Entities.Gdpr.GdprService>();
        services.AddScoped<EnterpriseAgentOs.Api.Entities.AgentTemplates.IAgentTemplateService, EnterpriseAgentOs.Api.Entities.AgentTemplates.AgentTemplateService>();
        services.AddScoped<EnterpriseAgentOs.Api.Entities.AgentLogs.IAgentLogService, EnterpriseAgentOs.Api.Entities.AgentLogs.AgentLogService>();
        return services;
    }

    public static IServiceCollection AddBackgroundServices(this IServiceCollection services)
    {
        return services;
    }

    public static IServiceCollection AddProtectors(this IServiceCollection services)
    {
        services.AddSingleton<EnterpriseAgentOs.Api.Entities.Providers.ProviderKeyProtector>();
        services.AddSingleton<EnterpriseAgentOs.Api.Entities.Skills.SkillCredentialProtector>();
        services.AddSingleton<EnterpriseAgentOs.Api.Entities.Channels.ChannelConfigProtector>();
        return services;
    }

    public static IServiceCollection AddHttpClients(this IServiceCollection services)
    {
        services.AddHttpClient<EnterpriseAgentOs.Api.Entities.Skills.SkillRuntimeClient>();
        // PostHog is HttpClient-first — registering it here gives us a typed
        // HttpClient and also registers IPostHogService -> PostHogService in
        // DI, so no separate AddScoped call is required.
        services.AddHttpClient<EnterpriseAgentOs.Api.Entities.PostHog.IPostHogService, EnterpriseAgentOs.Api.Entities.PostHog.PostHogService>();
        services.AddHttpClient("agent-proxy");
        services.AddHttpClient("llm-proxy");
        services.AddHttpClient("channel-platform");
        return services;
    }
}
