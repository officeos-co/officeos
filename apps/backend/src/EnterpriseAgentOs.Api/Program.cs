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

// ── Config helper: reads PascalCase (appsettings) or UPPER_SNAKE (Doppler env vars) ──
string Require(string pascalKey, string? envKey = null)
{
    var value = builder.Configuration[pascalKey];
    if (string.IsNullOrWhiteSpace(value) && envKey is not null)
        value = builder.Configuration[envKey];
    if (string.IsNullOrWhiteSpace(value))
        throw new InvalidOperationException(
            $"Required config '{pascalKey}' is missing. Set it in appsettings.json or as env var '{envKey ?? pascalKey}'.");
    return value;
}

T RequireSection<T>(string sectionName) where T : new()
{
    var config = new T();
    builder.Configuration.GetSection(sectionName).Bind(config);
    return config;
}

void RequireNotEmpty(string value, string name)
{
    if (string.IsNullOrWhiteSpace(value))
        throw new InvalidOperationException($"Required config '{name}' is missing or empty.");
}

var isDevelopment = builder.Environment.IsDevelopment();

// ── Core services ────────────────────────────────────────────────────
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Services.AddMemoryCache();

var redis = Require("Redis", "REDIS");
builder.Services.AddStackExchangeRedisCache(options =>
{
    options.Configuration = redis;
    options.InstanceName = "eaos:";
});

// Data Protection
var dpKeyPath = Require("DataProtectionKeyPath", "DATA_PROTECTION_KEY_PATH");
var dpKeyDir = System.IO.Path.IsPathRooted(dpKeyPath)
    ? dpKeyPath
    : System.IO.Path.Combine(Directory.GetCurrentDirectory(), dpKeyPath);
Directory.CreateDirectory(dpKeyDir);
builder.Services
    .AddDataProtection()
    .PersistKeysToFileSystem(new DirectoryInfo(dpKeyDir))
    .SetApplicationName("EnterpriseAgentOs.Api");

// DI — each layer registers its own services
var connectionString = Require("ConnectionString", "CONNECTION_STRING");
builder.Services.AddInfrastructure(connectionString);
builder.Services.AddApplication();
builder.Services.AddMediatR(cfg => cfg.RegisterServicesFromAssemblies(
    typeof(EnterpriseAgentOs.Application.ApplicationServiceRegistration).Assembly,
    typeof(Program).Assembly));

// ── Infrastructure configs ───────────────────────────────────────────
var kubernetesConfig = RequireSection<KubernetesConfig>("Kubernetes");
builder.Services.AddSingleton(kubernetesConfig);

// Docker is dev-only — uses hardcoded defaults from appsettings.json, never from env vars
var dockerConfig = new DockerConfig();
if (isDevelopment)
    builder.Configuration.GetSection("Docker").Bind(dockerConfig);
builder.Services.AddSingleton(dockerConfig);

var skillGatewayConfig = RequireSection<SkillGatewayConfig>("SkillGateway");
if (skillGatewayConfig.RefreshSeconds == 0) skillGatewayConfig.RefreshSeconds = 30;
builder.Services.AddSingleton(skillGatewayConfig);

var skillRuntimeConfig = RequireSection<SkillRuntimeConfig>("SkillRuntime");
RequireNotEmpty(skillRuntimeConfig.Url, "SkillRuntime:Url");
builder.Services.AddSingleton(skillRuntimeConfig);

var googleOAuthConfig = RequireSection<GoogleOAuthConfig>("GoogleOAuth");
RequireNotEmpty(googleOAuthConfig.ClientId, "GoogleOAuth:ClientId");
RequireNotEmpty(googleOAuthConfig.ClientSecret, "GoogleOAuth:ClientSecret");
builder.Services.AddSingleton(googleOAuthConfig);

var skillStorageConfig = RequireSection<SkillStorageConfig>("Minio");
RequireNotEmpty(skillStorageConfig.Endpoint, "Minio:Endpoint");
RequireNotEmpty(skillStorageConfig.AccessKey, "Minio:AccessKey");
RequireNotEmpty(skillStorageConfig.SecretKey, "Minio:SecretKey");
builder.Services.AddSingleton(skillStorageConfig);
builder.Services.AddSingleton<IAmazonS3>(_ =>
{
    var config = new AmazonS3Config
    {
        ServiceURL = skillStorageConfig.Endpoint,
        ForcePathStyle = true,
    };
    return new AmazonS3Client(
        skillStorageConfig.AccessKey,
        skillStorageConfig.SecretKey,
        config);
});

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
else if (isDevelopment && dockerConfig.Enabled)
{
    builder.Services.AddScoped<IAgentDeployer, DockerAgentDeployer>();
}
else
{
    throw new InvalidOperationException(
        "No agent deployer configured. Enable Kubernetes (production) or Docker (development) in config.");
}

// Billing
var stripeConfig = RequireSection<StripeConfig>("Stripe");
if (!isDevelopment)
{
    RequireNotEmpty(stripeConfig.SecretKey, "Stripe:SecretKey");
    RequireNotEmpty(stripeConfig.WebhookSecret, "Stripe:WebhookSecret");
}
builder.Services.AddSingleton(stripeConfig);

var frontendOrigin = Require("FrontendOrigin", "FRONTEND_ORIGIN");
var frontendConfig = new FrontendConfig(frontendOrigin);
builder.Services.AddSingleton(frontendConfig);

// LLM
builder.Services.AddSingleton(builder.Configuration.GetSection("PlatformKeys").Get<PlatformKeysConfig>() ?? new PlatformKeysConfig());

// Session auth — configurable skip prefixes
var sessionAuthConfig = RequireSection<SessionAuthConfig>("SessionAuth");
builder.Services.AddSingleton(sessionAuthConfig);

// PostHog
var postHogConfig = RequireSection<PostHogConfig>("PostHog");
builder.Services.AddSingleton(postHogConfig);

// GraphQL — two named schemas share one HotChocolate host:
//   "agent"     /api/graphql           → agent-pod skill gateway, dynamic per-skill fields
//   "dashboard" /api/graphql-dashboard → dashboard operator API, static per-domain fields
builder.Services.AddHttpContextAccessor();
builder.Services.AddSingleton<SkillTypeModule>();

builder.Services
    .AddGraphQLServer("agent")
    .AddQueryType<Query>()
    .AddMutationType<AgentSchemaMutations>()
    .AddTypeModule<SkillTypeModule>()
    .AddHttpRequestInterceptor<AgentAuthInterceptor>()
    .DisableIntrospection(false)
    .SetIntrospectionAllowedDepth(20, 20);

var dashboardGql = builder.Services
    .AddGraphQLServer("dashboard")
    .AddQueryType<GraphQLQueries>()
    .AddMutationType<GraphQLMutations>()
    .AddSubscriptionType<GraphQLSubscriptions>()
    .AddInMemorySubscriptions();
GraphQLRegistrationExtensions.AddDomainTypeExtensions(
    dashboardGql, typeof(Program).Assembly)
    .UseField<DashboardAuthMiddleware>()
    .DisableIntrospection(false);

// CORS
builder.Services.AddCors(options =>
{
    options.AddPolicy(FrontendCorsPolicy, policy =>
    {
        policy
            .WithOrigins(frontendOrigin)
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
    var providerRepo = scope.ServiceProvider.GetRequiredService<IProviderRepository>();
    await ProviderSeeder.SeedAsync(providerRepo);
    await SkillSeeder.SeedAsync(scope.ServiceProvider);
    await AgentTemplateSeeder.SeedAsync(scope.ServiceProvider);
}

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseMiddleware<CorrelationIdMiddleware>();
app.UseMiddleware<RequestResponseLoggingMiddleware>();
app.UseSerilogRequestLogging(options =>
{
    options.GetLevel = (httpContext, elapsed, ex) =>
    {
        var path = httpContext.Request.Path.Value ?? "";
        if (path is "/api/health")
            return LogEventLevel.Verbose;
        return LogEventLevel.Information;
    };
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

app.UseRouting();
app.UseCors(FrontendCorsPolicy);
app.UseMiddleware<SessionAuthMiddleware>();

app.UseWebSockets();

app.MapGet("/api/health", () => Results.Ok(new { ok = true }));
app.MapPost("/api/channels/inbound", ChannelInboundEndpoint.Handle);
app.MapGet("/api/channels/active", ChannelActiveEndpoint.Handle);

app.MapGraphQL("/api/graphql", schemaName: "agent");
app.MapGraphQL("/api/dashboard/graphql", schemaName: "dashboard")
    .RequireCors(FrontendCorsPolicy);
app.MapControllers();
app.Run();

public partial class Program { }
