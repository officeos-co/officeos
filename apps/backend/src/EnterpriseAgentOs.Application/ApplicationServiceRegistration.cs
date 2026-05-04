using Microsoft.Extensions.DependencyInjection;

namespace EnterpriseAgentOs.Application;

public static class ApplicationServiceRegistration
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        services.AddScoped<IAgentService, AgentService>();
        services.AddScoped<IProviderService, ProviderService>();
        services.AddScoped<IMcpServerService, McpServerService>();
        services.AddScoped<IUserBillingService, UserBillingService>();
        services.AddScoped<IOrgBillingService, OrgBillingService>();
        services.AddScoped<ICreditRecordingService, CreditRecordingService>();
        services.AddScoped<IBillingGuard, BillingGuard>();
        services.AddScoped<IGdprService, GdprService>();
        services.AddScoped<IAgentLogService, AgentLogService>();
        services.AddScoped<IChannelService, ChannelService>();
        services.AddScoped<IBrowserService, BrowserService>();
        services.AddScoped<IBrowserToolContextFactory, BrowserToolContextFactory>();
        services.AddSingleton<AgentTaskStore>();
        services.AddScoped<ToolRegistryFactory>();
        services.AddScoped<IAgentToolCatalogService, AgentToolCatalogService>();
        services.AddScoped<ConversationCompactionService>();
        services.AddScoped<AgentChannelBinder>();
        services.AddSingleton<ChannelReplyContext>();
        services.AddScoped<IAuthService, AuthService>();
        services.AddScoped<IOrganizationService, OrganizationService>();
        services.AddScoped<AgentTurnService>();

        // Background services
        services.AddHostedService<CronJobSchedulerService>();

        return services;
    }
}
