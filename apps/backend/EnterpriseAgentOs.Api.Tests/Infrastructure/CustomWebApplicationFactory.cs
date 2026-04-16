namespace EnterpriseAgentOs.Api.Tests.Infrastructure;

public class CustomWebApplicationFactory : WebApplicationFactory<Program>, IAsyncLifetime
{
    static CustomWebApplicationFactory()
    {
        Environment.SetEnvironmentVariable("ASPNETCORE_ENVIRONMENT", "Production");
    }

    private readonly PostgreSqlContainer _postgres = new PostgreSqlBuilder()
        .WithImage("postgres:16-alpine")
        .Build();

    public WireMockServer SkillRuntimeMock { get; } = WireMockServer.Start();
    public WireMockServer LiteLlmMock { get; } = WireMockServer.Start();

    public string PostgresConnectionString => _postgres.GetConnectionString();

    public async Task InitializeAsync()
    {
        await _postgres.StartAsync();

        // Build the base in-memory config dictionary.
        var inMemoryConfig = new Dictionary<string, string?>
        {
            ["Production:ConnectionString"] = _postgres.GetConnectionString(),
            ["Production:Kubernetes:Enabled"] = "false",
            ["Production:Kubernetes:Namespace"] = "default",
            ["Production:Kubernetes:Image"] = "harkro123/zeroclaw:latest",
            ["Production:SkillRuntimeUrl"] = SkillRuntimeMock.Url!,
            ["Production:SkillGatewayUrl"] = SkillRuntimeMock.Url!,
            ["Production:FrontendOrigin"] = "http://localhost:5173",
            ["Production:DataProtectionKeyPath"] = Path.Combine(Path.GetTempPath(), $"dp-keys-{Guid.NewGuid():N}"),
            ["Production:CouchDbUrl"] = "http://localhost:5984",
            ["Production:CouchDbUser"] = "test",
            ["Production:CouchDbPassword"] = "test",
            ["Production:GoogleOAuthClientId"] = "test-client-id",
            ["Production:GoogleOAuthClientSecret"] = "test-client-secret",
            ["Production:GoogleOAuthRedirectUri"] = "http://localhost/api/auth/callback/google",
            ["Production:MinioEndpoint"] = "http://localhost:9000",
            ["Production:MinioAccessKey"] = "testkey",
            ["Production:MinioSecretKey"] = "testsecret",
            ["Production:MinioBucket"] = "test-skills",
            ["Production:WorkOsApiKey"] = "WORKOS_API_KEY_PLACEHOLDER",
            ["Production:WorkOsClientId"] = "WORKOS_CLIENT_ID_PLACEHOLDER",
            ["Production:WorkOsRedirectUri"] = "https://api.officeos.co/api/sso/callback",
            ["Production:WorkOsEnabled"] = "false",
            ["Production:Stripe:SecretKey"] = "STRIPE_SECRET_KEY_PLACEHOLDER",
            ["Production:Stripe:WebhookSecret"] = "STRIPE_WEBHOOK_SECRET_PLACEHOLDER",
            ["Production:Stripe:FreePriceId"] = "STRIPE_FREE_PRICE_ID_PLACEHOLDER",
            ["Production:Stripe:TeamMonthlyPriceId"] = "STRIPE_TEAM_MONTHLY_PRICE_ID_PLACEHOLDER",
            ["Production:Stripe:TeamYearlyPriceId"] = "STRIPE_TEAM_YEARLY_PRICE_ID_PLACEHOLDER",
            ["Production:Stripe:TeamOveragePriceId"] = "STRIPE_TEAM_OVERAGE_PRICE_ID_PLACEHOLDER",
            ["Production:Stripe:Enabled"] = "false",
            ["Production:LiteLlm:BaseUrl"] = "http://localhost:4000",
            ["Production:LiteLlm:Enabled"] = "true",
            ["Production:PlatformKeys:AnthropicApiKey"] = "ANTHROPIC_API_KEY_PLACEHOLDER",
            ["Production:PlatformKeys:GeminiApiKey"] = "GEMINI_API_KEY_PLACEHOLDER",
            ["Production:PlatformKeys:XaiApiKey"] = "XAI_API_KEY_PLACEHOLDER",
            ["Production:PlatformKeys:OpenAiApiKey"] = "OPENAI_PLATFORM_API_KEY_PLACEHOLDER",
        };

        // Allow subclasses to inject additional or override config keys
        // (e.g. low rate limits for fast testing) before ValueManager is seeded.
        SetAdditionalTestConfig(inMemoryConfig);

        // Inject test config into ValueManager BEFORE Program.cs runs
        var testConfig = new ConfigurationBuilder()
            .SetBasePath(Directory.GetCurrentDirectory())
            .AddJsonFile("appsettings.json", optional: true)
            .AddInMemoryCollection(inMemoryConfig)
            .Build();

        EnterpriseAgentOs.Api.Properties.ValueManager.SetConfiguration(testConfig);

        // Seed WireMock with a default manifests endpoint (empty list)
        SkillRuntimeMock
            .Given(Request.Create().WithPath("/manifests").UsingGet())
            .RespondWith(Response.Create()
                .WithStatusCode(200)
                .WithHeader("Content-Type", "application/json")
                .WithBody("[]"));
    }

    async Task IAsyncLifetime.DisposeAsync()
    {
        SkillRuntimeMock.Stop();
        LiteLlmMock.Stop();
        await _postgres.DisposeAsync();
    }

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Production");

        builder.ConfigureServices(services =>
        {
            // Replace EF Core to use Testcontainers Postgres
            services.RemoveAll<DbContextOptions<EnterpriseAgentOs.Api.Database.EaosDbContext>>();
            services.RemoveAll<EnterpriseAgentOs.Api.Database.EaosDbContext>();
            services.AddDbContext<EnterpriseAgentOs.Api.Database.EaosDbContext>(options =>
                options.UseNpgsql(_postgres.GetConnectionString()));

            // Replace vault client with a no-op stub
            services.RemoveAll<EnterpriseAgentOs.Api.Entities.Vault.IVaultClient>();
            services.AddScoped<EnterpriseAgentOs.Api.Entities.Vault.IVaultClient, StubVaultClient>();

            // Replace SkillRuntimeConfig to point at WireMock
            services.RemoveAll<EnterpriseAgentOs.Api.Properties.SkillRuntimeConfig>();
            services.AddSingleton(new EnterpriseAgentOs.Api.Properties.SkillRuntimeConfig { Url = SkillRuntimeMock.Url! });

            // Replace LiteLlmConfig to point at the WireMock LiteLLM stub
            services.RemoveAll<EnterpriseAgentOs.Api.Properties.LiteLlmConfig>();
            services.AddSingleton(new EnterpriseAgentOs.Api.Properties.LiteLlmConfig { BaseUrl = LiteLlmMock.Url!, Enabled = true });
        });
    }

    /// <summary>
    /// Override in a subclass to inject additional config keys or override defaults
    /// before ValueManager is seeded. Called once per factory instance during
    /// <see cref="InitializeAsync"/>.
    /// </summary>
    protected virtual void SetAdditionalTestConfig(IDictionary<string, string?> config) { }
}
