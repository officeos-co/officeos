namespace OffceOs.Extensions;

public static class InfrastructureServiceCollectionExtensions
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, string connectionString)
    {
        // DbContext
        services.AddDbContext<EaosDbContext>(options =>
            options.UseNpgsql(connectionString));

        // Repositories
        services.AddScoped<IAgentRepository, AgentRepository>();
        services.AddScoped<IAgentDefinitionRepository, AgentDefinitionRepository>();

        services.AddScoped<IBrowserSessionRepository, BrowserSessionRepository>();
        services.AddScoped<IIntegrationDefinitionRepository, IntegrationDefinitionRepository>();
        services.AddScoped<IAgentIntegrationRepository, AgentIntegrationRepository>();
        services.AddScoped<IIntegrationCredentialRepository, IntegrationCredentialRepository>();
        services.AddScoped<IIntegrationDeploymentRepository, IntegrationDeploymentRepository>();
        services.AddScoped<IIntegrationConnectionRepository, IntegrationConnectionRepository>();
        services.AddScoped<IIntegrationIndexEntityStatusRepository, IntegrationIndexEntityStatusRepository>();
        services.AddScoped<IIntegrationIndexJobRepository, IntegrationIndexJobRepository>();
        services.AddScoped<IIntegrationIndexedRecordRepository, IntegrationIndexedRecordRepository>();
        services.AddScoped<IIntegrationActivityRepository, IntegrationActivityRepository>();
        services.AddScoped<IIntegrationRequestHistoryRepository, IntegrationRequestHistoryRepository>();
        services.AddScoped<IMemoryStoreRepository, MemoryStoreRepository>();
        services.AddScoped<IOAuthTokenRepository, OAuthTokenRepository>();
        services.AddScoped<IUserRepository, UserRepository>();
        services.AddScoped<IWorkspaceRepository, WorkspaceRepository>();
        services.AddScoped<IWorkspaceMemberRepository, WorkspaceMemberRepository>();
        services.AddScoped<IAccessGroupRepository, AccessGroupRepository>();
        services.AddScoped<ISessionRepository, SessionRepository>();
        services.AddScoped<IChannelRepository, ChannelRepository>();
        services.AddScoped<IAgentLogRepository, AgentLogRepository>();
        services.AddScoped<IAgentUsageRepository, AgentUsageRepository>();
        services.AddScoped<IOrganizationRepository, OrganizationRepository>();
        services.AddScoped<IOrganizationAuditLogRepository, OrganizationAuditLogRepository>();
        services.AddScoped<IOrganizationPolicyProfileRepository, OrganizationPolicyProfileRepository>();
        services.AddScoped<IOrganizationProviderProfileRepository, OrganizationProviderProfileRepository>();
        services.AddScoped<IUserSubscriptionRepository, UserSubscriptionRepository>();
        services.AddScoped<IOrgSubscriptionRepository, OrgSubscriptionRepository>();
        services.AddScoped<IAgentMemoryRepository, AgentMemoryRepository>();
        services.AddScoped<IAgentPersonalityRepository, AgentPersonalityRepository>();
        services.AddScoped<IAgentRoutineRepository, AgentRoutineRepository>();
        services.AddScoped<IAgentSessionRepository, AgentSessionRepository>();
        services.AddScoped<IAgentSessionContextRepository, AgentSessionContextRepository>();
        services.AddScoped<IAgentRunRepository, AgentRunRepository>();
        services.AddScoped<IAgentResourceRepository, AgentResourceRepository>();

        // Adapters
        services.AddScoped<IChannelGateway, ChannelSidecarGateway>();
        services.AddScoped<LlmProviderDispatcher>();
        services.AddScoped<ICloudProviderTokenService, CloudProviderTokenService>();
        services.AddScoped<IStripeWebhookService, StripeWebhookService>();
        services.AddScoped<IStripeMeteringService, StripeMeteringService>();
        // Adapters - integrations
        services.AddSingleton<IIntegrationClientManager, IntegrationClientManager>();

        // Protectors
        services.AddSingleton<CredentialProtector>();
        services.AddSingleton<ChannelCredentialProtector>();
        services.AddScoped<IIntegrationCredentialEncryptionService, IntegrationCredentialEncryptionService>();

        // HTTP clients
        services.AddHttpClient<IPostHogService, PostHogService>();
        services.AddHttpClient("vault-transit");
        services.AddHttpClient("agent-proxy");
        services.AddHttpClient("llm-proxy");
        services.AddHttpClient<IBrowserRuntimeClient, AutoBrowserRuntimeClient>();
        services.AddHttpClient("github-integration-indexing", client =>
        {
            client.BaseAddress = new Uri("https://api.github.com/");
            client.DefaultRequestHeaders.UserAgent.ParseAdd("OfficeOS-Integration/1.0");
            client.DefaultRequestHeaders.Accept.ParseAdd("application/vnd.github+json");
        });
        services.AddHttpClient("channel-sidecar", client =>
        {
            var channelUrl = Environment.GetEnvironmentVariable("CHANNEL_SERVICE_URL")
                ?? throw new InvalidOperationException("Missing required env var: CHANNEL_SERVICE_URL");
            client.BaseAddress = new Uri(channelUrl);
        });

        return services;
    }
}
