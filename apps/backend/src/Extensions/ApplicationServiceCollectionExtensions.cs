namespace OffceOs.Extensions;

public static class ApplicationServiceCollectionExtensions
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        services.AddScoped<IAgentService, AgentService>();
        services.AddScoped<IAgentDashboardService, AgentDashboardService>();
        services.AddScoped<IAgentRoutineService, AgentRoutineService>();
        services.AddScoped<AgentRoutineExecutionService>();
        services.AddScoped<IAgentRoutineExecutionService>(provider => provider.GetRequiredService<AgentRoutineExecutionService>());
        services.AddScoped<IAgentSessionService, AgentSessionService>();
        services.AddScoped<IAgentResourceService, AgentResourceService>();
        services.AddScoped<ProviderService>();
        services.AddScoped<IProviderDispatchService, ProviderDispatchService>();
        services.AddScoped<IIntegrationDefinitionService, IntegrationDefinitionService>();
        services.AddScoped<IIntegrationDeploymentService, IntegrationDeploymentService>();
        services.AddScoped<IAgentMemoryService, AgentMemoryService>();
        services.AddScoped<IMemoryStoreService, MemoryStoreService>();
        services.AddScoped<GitHubIntegrationClient>();
        services.AddScoped<IGdprService, GdprService>();
        services.AddScoped<IOrganizationAuditLogService, OrganizationAuditLogService>();
        services.AddScoped<IAgentLogService, AgentLogService>();
        services.AddScoped<IControlPlaneRunService, OpenCodeRunService>();
        services.AddScoped<AgentDefinitionParser>();
        services.AddScoped<DeclarativeManifestParser>();
        services.AddScoped<IDeclarativeAgentService, DeclarativeAgentService>();
        services.AddScoped<IUsageAnalyticsService, UsageAnalyticsService>();
        services.AddScoped<IAgentUsageService, AgentUsageService>();
        services.AddScoped<IAgentUsageAnalyticsService, AgentUsageAnalyticsService>();
        services.AddScoped<IChannelService, ChannelService>();
        services.AddScoped<IBrowserService, BrowserService>();
        services.AddScoped<AgentChannelBinder>();
        services.AddSingleton<ChannelReplyContext>();
        services.AddScoped<IAuthService, AuthService>();
        services.AddScoped<ICliAuthService, CliAuthService>();
        services.AddScoped<IOrganizationService, OrganizationService>();
        services.AddScoped<IAccessGroupService, AccessGroupService>();
        services.AddScoped<IOrganizationPolicyService, OrganizationPolicyService>();
        services.AddScoped<IWorkspaceService, WorkspaceService>();

        // Background services
        services.AddHostedService<AgentRoutineSchedulerService>();
        services.AddHostedService<AgentRuntimeCleanupService>();
        services.AddHostedService<AgentRunDispatchService>();

        return services;
    }
}
