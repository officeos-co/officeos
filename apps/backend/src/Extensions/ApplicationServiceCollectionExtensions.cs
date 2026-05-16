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
        services.AddScoped<AgentRoutineGitHubPollerService.AgentRoutineGitHubPollingService>();
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
        services.AddScoped<AgentHarnessToolPermissionResolver>();
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
        services.AddSingleton<IControlPlaneResourceCatalogService, ControlPlaneResourceCatalogService>();
        services.AddControlPlaneResource("agents", "agent", ["agent"], "Agents", "Agent resources", "hubot", ["list", "describe", "delete", "logs"], ["name", "status", "provider", "model"]);
        services.AddControlPlaneResource("browsers", "browser", ["browser"], "Browsers", "Browser resources", "browser", ["list", "describe", "delete"], ["name", "displayName", "currentAgentId"]);
        services.AddControlPlaneResource("channels", "channel", ["channel"], "Channels", "Channel connections", "broadcast", ["list", "describe", "delete"], ["name", "platform", "enabled"]);
        services.AddControlPlaneResource("credentials", "credential", ["credential"], "Credentials", "Routine credentials", "key", ["list", "describe", "delete"], ["name", "provider", "authKind", "configured"]);
        services.AddControlPlaneResource("integrations", "integration", ["integration"], "Integrations", "Integration deployments", "plug", ["list", "describe", "delete"], ["name", "server", "status"]);
        services.AddControlPlaneResource("memory-stores", "memory-store", ["memory-store", "memorystore", "memorystores"], "Memory Stores", "Memory stores", "database", ["list", "describe", "delete"], ["name", "entryCount", "updatedAt"]);
        services.AddControlPlaneResource("models", "model", ["model"], "Models", "Provider models", "symbol-method", ["list", "describe"], ["id", "displayName", "provider"]);
        services.AddControlPlaneResource("providers", "provider", ["provider"], "Providers", "Configured provider resources", "server-process", ["list", "describe", "delete"], ["name", "type", "configured", "phase"]);

        // Background services
        services.AddHostedService<AgentRoutineSchedulerService>();
        services.AddHostedService<AgentRoutineGitHubPollerService>();
        services.AddHostedService<AgentRuntimeCleanupService>();

        return services;
    }

    private static IServiceCollection AddControlPlaneResource(
        this IServiceCollection services,
        string kind,
        string singular,
        IReadOnlyList<string> aliases,
        string displayName,
        string description,
        string icon,
        IReadOnlyList<string> capabilities,
        IReadOnlyList<string> displayFields)
    {
        services.AddSingleton<IControlPlaneResourceCatalogProvider>(new StaticControlPlaneResourceCatalogProvider(
            new ControlPlaneResourceDescriptor(kind, singular, aliases, displayName, description, icon, capabilities, displayFields)));

        return services;
    }
}
