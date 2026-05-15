namespace OffceOs.Extensions;

public static class ApplicationServiceCollectionExtensions
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        services.AddScoped<IAgentService, AgentService>();
        services.AddScoped<IAgentLifecycleService, AgentLifecycleService>();
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
        services.AddScoped<IAgentLogService, AgentLogService>();
        services.AddScoped<IAgentHarnessService, AgentHarnessService>();
        services.AddScoped<TurnEventPublisher>();
        services.AddScoped<ConversationCompactionService>();
        services.AddScoped<TurnContextBuilder>();
        services.AddScoped<LlmRequestBuilder>();
        services.AddScoped<SseResponseParser>();
        services.AddScoped<LlmTurnExecutor>();
        services.AddScoped<ToolRegistryFactory>();
        services.AddScoped<ToolExecutionLoop>();
        services.AddScoped<AgentHarnessToolPermissionPolicy>();
        services.AddScoped<IBrowserToolContextFactory, BrowserToolContextFactory>();
        services.AddScoped<IBrowserToolService, BrowserToolService>();
        services.AddScoped<IAgentToolCatalogService, AgentToolCatalogService>();
        services.AddSingleton<AgentTaskStore>();
        services.AddScoped<AgentDefinitionParser>();
        services.AddScoped<DeclarativeManifestParser>();
        services.AddScoped<IDeclarativeAgentService, DeclarativeAgentService>();
        services.AddScoped<IChannelService, ChannelService>();
        services.AddScoped<IBrowserService, BrowserService>();
        services.AddScoped<IBrowserResourceService, BrowserResourceService>();
        services.AddScoped<AgentChannelBinder>();
        services.AddSingleton<ChannelReplyContext>();
        services.AddScoped<IAuthService, AuthService>();
        services.AddScoped<ICliAuthService, CliAuthService>();
        services.AddScoped<IWorkspaceService, WorkspaceService>();

        // Background services
        services.AddHostedService<AgentRoutineSchedulerService>();
        services.AddHostedService<AgentRuntimeCleanupService>();

        return services;
    }
}
