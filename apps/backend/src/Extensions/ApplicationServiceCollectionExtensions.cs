using OffceOs.Features.AgentDefinitions.Application;
using OffceOs.Features.AgentHarness.Application;
using OffceOs.Features.AgentHarness.Application.BrowserTools;
using OffceOs.Features.AgentHarness.Application.Tools;
using OffceOs.Features.AgentRoutines.Application;
using OffceOs.Features.Agents.Application;
using OffceOs.Features.Browser.Application;
using OffceOs.Features.Channels.Application;
using OffceOs.Features.Context.Application;
using OffceOs.Features.ControlPlane.Application;
using OffceOs.Features.Integrations.Application;
using OffceOs.Features.Management.Application;
using OffceOs.Features.Providers.Application;
using OffceOs.Features.ResourceLogs.Application;
using OffceOs.Features.Channels.Domain;
using OffceOs.Features.Context.Domain;
using OffceOs.Features.Integrations.Domain;
using OffceOs.Features.Management.Domain;

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
        services.AddScoped<IResourceLogService, ResourceLogService>();
        services.AddScoped<IResourceLogWriterService, ResourceLogWriterService>();
        services.AddScoped<IAgentWorkQueueService, AgentWorkQueueService>();
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
        services.AddScoped<IControlPlaneResourceService, ControlPlaneResourceService>();
        services.AddScoped<IControlPlaneResourceResolver, AgentControlPlaneResourceResolver>();
        services.AddScoped<IControlPlaneResourceResolver, BrowserControlPlaneResourceResolver>();
        services.AddScoped<IControlPlaneResourceResolver, ChannelControlPlaneResourceResolver>();
        services.AddScoped<IControlPlaneResourceResolver, ControlPlaneControlPlaneResourceResolver>();
        services.AddScoped<IControlPlaneResourceResolver, CredentialControlPlaneResourceResolver>();
        services.AddScoped<IControlPlaneResourceResolver, IntegrationControlPlaneResourceResolver>();
        services.AddScoped<IControlPlaneResourceResolver, MemoryStoreControlPlaneResourceResolver>();
        services.AddScoped<IControlPlaneResourceResolver, ModelControlPlaneResourceResolver>();
        services.AddScoped<IControlPlaneResourceResolver, ProviderControlPlaneResourceResolver>();
        services.AddScoped<IControlPlaneResourceResolver, RoutineControlPlaneResourceResolver>();
        services.AddScoped<IControlPlaneResourceResolver, SessionControlPlaneResourceResolver>();

        // Background services
        services.AddHostedService<AgentRoutineSchedulerService>();
        services.AddHostedService<AgentRoutineGitHubPollerService>();
        services.AddHostedService<AgentRuntimeCleanupService>();

        return services;
    }
}
