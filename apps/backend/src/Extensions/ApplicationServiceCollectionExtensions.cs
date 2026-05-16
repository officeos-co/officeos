using OffceOs.Application.Features.AgentDefinitions;
using OffceOs.Application.Features.AgentHarness;
using OffceOs.Application.Features.AgentRoutines;
using OffceOs.Application.Features.Agents;
using OffceOs.Application.Features.Browser;
using OffceOs.Application.Features.Channels;
using OffceOs.Application.Features.Context;
using OffceOs.Application.Features.ControlPlane;
using OffceOs.Application.Features.Integrations;
using OffceOs.Application.Features.Management;
using OffceOs.Application.Features.Providers;
using OffceOs.Application.Features.ResourceLogs;
using OffceOs.Domain.Features.Channels;
using OffceOs.Domain.Features.Context;
using OffceOs.Domain.Features.Integrations;
using OffceOs.Domain.Features.Management;
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

        // Background services
        services.AddHostedService<AgentRoutineSchedulerService>();
        services.AddHostedService<AgentRoutineGitHubPollerService>();
        services.AddHostedService<AgentRuntimeCleanupService>();

        return services;
    }
}
