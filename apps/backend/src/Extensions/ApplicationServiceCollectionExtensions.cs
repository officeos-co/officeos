namespace OffceOs.Extensions;

public static class ApplicationServiceCollectionExtensions
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        services.AddScoped<IAgentService, AgentService>();
        services.AddScoped<IAgentDashboardService, AgentDashboardService>();
        services.AddScoped<IAgentCronJobService, AgentCronJobService>();
        services.AddScoped<IAgentSessionService, AgentSessionService>();
        services.AddScoped<IAgentResourceService, AgentResourceService>();
        services.AddScoped<IProviderService, ProviderService>();
        services.AddScoped<IIntegrationDefinitionService, IntegrationDefinitionService>();
        services.AddScoped<IIntegrationConnectionService, IntegrationConnectionService>();
        services.AddScoped<IIntegrationExecutionService, IntegrationExecutionService>();
        services.AddScoped<IAgentMemoryService, AgentMemoryService>();
        services.AddScoped<IMemoryStoreService, MemoryStoreService>();
        services.AddScoped<GitHubIntegrationClient>();
        services.AddScoped<IntegrationIndexingService>();
        services.AddScoped<IUserBillingService, UserBillingService>();
        services.AddScoped<IOrgBillingService, OrgBillingService>();
        services.AddScoped<ICreditRecordingService, CreditRecordingService>();
        services.AddScoped<IBillingGuard, BillingGuard>();
        services.AddScoped<IGdprService, GdprService>();
        services.AddScoped<IAgentLogService, AgentLogService>();
        services.AddScoped<IUsageAnalyticsService, UsageAnalyticsService>();
        services.AddScoped<IChannelService, ChannelService>();
        services.AddScoped<IBrowserService, BrowserService>();
        services.AddScoped<IBrowserToolContextFactory, BrowserToolContextFactory>();
        services.AddSingleton<AgentTaskStore>();
        services.AddScoped<ToolRegistryFactory>();
        services.AddScoped<IAgentToolCatalogService, AgentToolCatalogService>();
        services.AddScoped<ConversationCompactionService>();
        services.AddScoped<AgentRunLifecycle>();
        services.AddScoped<TurnEventPublisher>();
        services.AddScoped<TurnContextBuilder>();
        services.AddScoped<BillingCheckpoint>();
        services.AddScoped<LlmRequestBuilder>();
        services.AddScoped<SseResponseParser>();
        services.AddScoped<UsageResolver>();
        services.AddScoped<LlmTurnExecutor>();
        services.AddScoped<ToolExecutionLoop>();
        services.AddScoped<AgentChannelBinder>();
        services.AddSingleton<ChannelReplyContext>();
        services.AddScoped<IAuthService, AuthService>();
        services.AddScoped<IOrganizationService, OrganizationService>();
        services.AddScoped<IWorkspaceService, WorkspaceService>();
        services.AddScoped<AgentTurnService>();

        // Background services
        services.AddHostedService<CronJobSchedulerService>();
        services.AddHostedService<IntegrationIndexSchedulerService>();
        services.AddHostedService<AgentRuntimeCleanupService>();

        return services;
    }
}
