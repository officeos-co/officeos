
using EnterpriseAgentOs.Api.Extensions;

const string FrontendCorsPolicy = "dashboard";

Log.Logger = new LoggerConfiguration()
    .MinimumLevel.Debug()
    .MinimumLevel.Override("Microsoft", LogEventLevel.Warning)
    .MinimumLevel.Override("Microsoft.Hosting.Lifetime", LogEventLevel.Information)
    .MinimumLevel.Override("Microsoft.EntityFrameworkCore", LogEventLevel.Warning)
    .Enrich.FromLogContext()
    .WriteTo.Console(
        outputTemplate: "[{Timestamp:HH:mm:ss} {Level:u3}] {Message:lj} {Properties:j}{NewLine}{Exception}")
    .CreateLogger();

var builder = WebApplication.CreateBuilder(args);
builder.Host.UseSerilog();

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// Data Protection
var dpKeyPath = ValueManager.GetValue<string>("DataProtectionKeyPath");
var dpKeyDir = System.IO.Path.IsPathRooted(dpKeyPath)
    ? dpKeyPath
    : System.IO.Path.Combine(Directory.GetCurrentDirectory(), dpKeyPath);
Directory.CreateDirectory(dpKeyDir);
builder.Services
    .AddDataProtection()
    .PersistKeysToFileSystem(new DirectoryInfo(dpKeyDir))
    .SetApplicationName("EnterpriseAgentOs.Api");

// Database
builder.Services.AddDbContext<EaosDbContext>(options =>
    options.UseNpgsql(ValueManager.GetValue<string>("ConnectionString")));

// DI — services, repositories, protectors, HTTP clients
builder.Services
    .AddRepositories()
    .AddApplicationServices()
    .AddBackgroundServices()
    .AddProtectors()
    .AddHttpClients();

// Infrastructure configs — bind from nested appsettings sections
var envSection = ValueManager.GetConfiguration().GetSection(ValueManager.GetEnvironmentName());

var kubernetesConfig = new KubernetesConfig();
envSection.GetSection("Kubernetes").Bind(kubernetesConfig);
builder.Services.AddSingleton(kubernetesConfig);

var couchDbConfig = new CouchDbConfig();
envSection.GetSection("CouchDb").Bind(couchDbConfig);
builder.Services.AddSingleton(couchDbConfig);

var skillGatewayConfig = new SkillGatewayConfig { RefreshSeconds = 30 };
envSection.GetSection("SkillGateway").Bind(skillGatewayConfig);
builder.Services.AddSingleton(skillGatewayConfig);

var skillRuntimeConfig = new SkillRuntimeConfig();
envSection.GetSection("SkillRuntime").Bind(skillRuntimeConfig);
builder.Services.AddSingleton(skillRuntimeConfig);

var googleOAuthConfig = new GoogleOAuthConfig();
envSection.GetSection("GoogleOAuth").Bind(googleOAuthConfig);
builder.Services.AddSingleton(googleOAuthConfig);

var workOsConfig = new WorkOsConfig();
envSection.GetSection("WorkOs").Bind(workOsConfig);
builder.Services.AddSingleton(workOsConfig);

var skillStorageConfig = new SkillStorageConfig();
envSection.GetSection("Minio").Bind(skillStorageConfig);
builder.Services.AddSingleton(skillStorageConfig);
builder.Services.AddSingleton<Amazon.S3.IAmazonS3>(_ =>
{
    var config = new Amazon.S3.AmazonS3Config
    {
        ServiceURL = skillStorageConfig.Endpoint,
        ForcePathStyle = true,
    };
    return new Amazon.S3.AmazonS3Client(
        skillStorageConfig.AccessKey,
        skillStorageConfig.SecretKey,
        config);
});

// Kubernetes deployer
if (kubernetesConfig.Enabled)
{
    builder.Services.AddSingleton<IKubernetes>(_ =>
    {
        var config = KubernetesClientConfiguration.IsInCluster()
            ? KubernetesClientConfiguration.InClusterConfig()
            : KubernetesClientConfiguration.BuildDefaultConfig();
        return new Kubernetes(config);
    });
    builder.Services.AddScoped<IAgentDeployer, KubernetesAgentDeployer>();
}
else
{
    builder.Services.AddScoped<IAgentDeployer, NullAgentDeployer>();
}

// Billing
var stripeConfig = new StripeConfig();
envSection.GetSection("Stripe").Bind(stripeConfig);
builder.Services.AddSingleton(stripeConfig);

var frontendConfig = new FrontendConfig(ValueManager.GetValue<string>("FrontendOrigin"));
builder.Services.AddSingleton(frontendConfig);

// LLM
builder.Services.AddSingleton(envSection.GetSection("LiteLlm").Get<LiteLlmConfig>() ?? new LiteLlmConfig());
builder.Services.AddSingleton(envSection.GetSection("PlatformKeys").Get<PlatformKeysConfig>() ?? new PlatformKeysConfig());

// PostHog — server owns the API key; dashboard-2 calls use-case-specific track* mutations
var postHogConfig = new PostHogConfig();
envSection.GetSection("PostHog").Bind(postHogConfig);
builder.Services.AddSingleton(postHogConfig);

// Rate limiting
var rateLimitingConfig = new RateLimitingConfig();
envSection.GetSection("RateLimiting").Bind(rateLimitingConfig);
builder.Services.AddSingleton(rateLimitingConfig);

// GraphQL — two named schemas share one HotChocolate host:
//   "agent"     /api/graphql           → agent-pod skill gateway, dynamic per-skill fields
//   "dashboard" /api/graphql-dashboard → dashboard operator API, static per-domain fields
builder.Services.AddHttpContextAccessor();
builder.Services.AddSingleton<EnterpriseAgentOs.Api.Entities.SkillGateway.SkillTypeModule>();

builder.Services
    .AddGraphQLServer("agent")
    .AddQueryType<EnterpriseAgentOs.Api.Entities.SkillGateway.Query>()
    .AddTypeModule<EnterpriseAgentOs.Api.Entities.SkillGateway.SkillTypeModule>()
    .AddHttpRequestInterceptor<EnterpriseAgentOs.Api.Entities.SkillGateway.AgentAuthInterceptor>()
    .DisableIntrospection(false)
    .SetIntrospectionAllowedDepth(20, 20);

builder.Services
    .AddGraphQLServer("dashboard")
    .AddQueryType<EnterpriseAgentOs.Api.GraphQLQueries>()
    .AddMutationType<EnterpriseAgentOs.Api.GraphQLMutations>()
    .AddSubscriptionType<EnterpriseAgentOs.Api.GraphQLSubscriptions>()
    .AddInMemorySubscriptions()
    .AddDomainTypeExtensions(typeof(Program).Assembly)
    .UseField<EnterpriseAgentOs.Api.Middleware.DashboardAuthMiddleware>()
    .DisableIntrospection(false);

// CORS
builder.Services.AddCors(options =>
{
    options.AddPolicy(FrontendCorsPolicy, policy =>
    {
        policy
            .WithOrigins(ValueManager.GetValue<string>("FrontendOrigin"))
            .AllowAnyHeader()
            .AllowAnyMethod()
            .AllowCredentials();
    });
});

var app = builder.Build();

using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<EaosDbContext>();
    await db.Database.MigrateAsync();
    await ProviderSeeder.SeedAsync(db);
    await SkillSeeder.SeedAsync(scope.ServiceProvider);
    await AgentTemplateSeeder.SeedAsync(scope.ServiceProvider);
}

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseMiddleware<CorrelationIdMiddleware>();
app.UseSerilogRequestLogging(options =>
{
    options.EnrichDiagnosticContext = (diagnosticContext, httpContext) =>
    {
        diagnosticContext.Set("RequestHost", httpContext.Request.Host.Value ?? "unknown");
        diagnosticContext.Set("UserAgent", httpContext.Request.Headers.UserAgent.ToString() ?? "");
        if (httpContext.Items.TryGetValue("agent-id", out var agentId) && agentId is not null)
            diagnosticContext.Set("AgentId", agentId);
        if (httpContext.Items["User"] is UserRecord user)
            diagnosticContext.Set("UserId", user.Id);
    };
});

app.UseCors(FrontendCorsPolicy);
app.UseMiddleware<SessionAuthMiddleware>();

app.UseWebSockets();

app.MapGet("/api/health", () => Results.Ok(new { ok = true }));
app.MapGet("/healthz", () => Results.Ok(new { ok = true }));

app.MapAgentProxyEndpoints();
app.MapGraphQL("/api/graphql", schemaName: "agent");
app.MapGraphQL("/api/dashboard/graphql", schemaName: "dashboard");
app.MapControllers();

app.Run();

// Make Program visible to test project (WebApplicationFactory<Program>)
public partial class Program { }
