string? FindRootEnvFile()
{
    var current = new DirectoryInfo(Directory.GetCurrentDirectory());
    while (current is not null)
    {
        var envPath = System.IO.Path.Combine(current.FullName, ".env");
        if (System.IO.File.Exists(System.IO.Path.Combine(current.FullName, "docker-compose.yml"))
            || Directory.Exists(System.IO.Path.Combine(current.FullName, ".git")))
            return System.IO.File.Exists(envPath) ? envPath : null;

        current = current.Parent;
    }

    return null;
}

static bool IsDevelopmentEnvironmentName(string? environmentName) =>
    string.Equals(environmentName, Environments.Development, StringComparison.OrdinalIgnoreCase);

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

var explicitEnvironment =
    Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT")
    ?? Environment.GetEnvironmentVariable("DOTNET_ENVIRONMENT");
var rootEnvFile = FindRootEnvFile();
if (IsDevelopmentEnvironmentName(explicitEnvironment)
    || (string.IsNullOrWhiteSpace(explicitEnvironment) && rootEnvFile is not null))
{
    if (rootEnvFile is null)
        throw new InvalidOperationException("Root .env file not found.");

    dotenv.net.DotEnv.Fluent().WithoutExceptions().WithEnvFiles(rootEnvFile).Load();
}

var builder = WebApplication.CreateBuilder(args);
builder.Host.UseSerilog();
var isDevelopment = builder.Environment.IsDevelopment();

// Only local development reads .env. Staging and Production get configuration
// from Kubernetes secrets and environment variables.
if (isDevelopment)
{
    if (rootEnvFile is null)
        throw new InvalidOperationException("Root .env file not found.");

    builder.Configuration.AddEnvironmentVariables();
}


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

// ── Core services ────────────────────────────────────────────────────
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

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

var googleOAuthConfig = RequireSection<GoogleOAuthConfig>("GoogleOAuth");
builder.Services.AddSingleton(googleOAuthConfig);

var gitHubOAuthConfig = RequireSection<GitHubOAuthConfig>("GitHubOAuth");
builder.Services.AddSingleton(gitHubOAuthConfig);

var kubernetesConfig = RequireSection<KubernetesConfig>("Kubernetes");
builder.Services.AddSingleton(kubernetesConfig);

var dockerConfig = RequireSection<DockerConfig>("Docker");
builder.Services.AddSingleton(dockerConfig);

var workspaceStorageConfig = RequireSection<WorkspaceStorageConfig>("WorkspaceStorage");
RequireNotEmpty(workspaceStorageConfig.Endpoint, "WorkspaceStorage:Endpoint");
RequireNotEmpty(workspaceStorageConfig.AccessKey, "WorkspaceStorage:AccessKey");
RequireNotEmpty(workspaceStorageConfig.SecretKey, "WorkspaceStorage:SecretKey");
RequireNotEmpty(workspaceStorageConfig.Bucket, "WorkspaceStorage:Bucket");
builder.Services.AddSingleton(workspaceStorageConfig);
builder.Services.AddSingleton<PodExecutorClient>();
builder.Services.AddSingleton<IAgentWorkspaceStore, S3AgentWorkspaceStore>();

if (!isDevelopment)
{
    RequireNotEmpty(kubernetesConfig.Namespace, "Kubernetes:Namespace");
    RequireNotEmpty(kubernetesConfig.Image, "Kubernetes:Image");

    builder.Services.AddSingleton<IKubernetes>(_ =>
    {
        var config = KubernetesClientConfiguration.IsInCluster()
            ? KubernetesClientConfiguration.InClusterConfig()
            : KubernetesClientConfiguration.BuildDefaultConfig();
        return new Kubernetes(config);
    });
    builder.Services.AddScoped<KubernetesAgentSandbox>();
    builder.Services.AddScoped<IAgentSandbox>(sp => sp.GetRequiredService<KubernetesAgentSandbox>());
    builder.Services.AddScoped<IAgentDeployer>(sp => sp.GetRequiredService<KubernetesAgentSandbox>());
    builder.Services.AddScoped<IAgentRuntimeCleaner>(sp => sp.GetRequiredService<KubernetesAgentSandbox>());
}
else
{
    RequireNotEmpty(dockerConfig.Image, "Docker:Image");
    RequireNotEmpty(dockerConfig.Network, "Docker:Network");
    RequireNotEmpty(dockerConfig.SocketPath, "Docker:SocketPath");

    builder.Services.AddScoped<DockerAgentSandbox>();
    builder.Services.AddScoped<IAgentSandbox>(sp => sp.GetRequiredService<DockerAgentSandbox>());
    builder.Services.AddScoped<IAgentDeployer>(sp => sp.GetRequiredService<DockerAgentSandbox>());
    builder.Services.AddScoped<IAgentRuntimeCleaner>(sp => sp.GetRequiredService<DockerAgentSandbox>());
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
var platformKeysConfig = RequireSection<PlatformKeysConfig>("PlatformKeys");
builder.Services.AddSingleton(platformKeysConfig);

// Session auth — configurable skip prefixes
var sessionAuthConfig = RequireSection<SessionAuthConfig>("SessionAuth");
builder.Services.AddSingleton(sessionAuthConfig);

var browserRuntimeConfig = new BrowserRuntimeConfig
{
    BaseUrl = builder.Configuration["BrowserRuntime:BaseUrl"]
        ?? builder.Configuration["BROWSER_SERVICE_URL"]
        ?? "http://browser:8000",
    PublicViewBaseUrl = builder.Configuration["BrowserRuntime:PublicViewBaseUrl"]
        ?? builder.Configuration["BROWSER_PUBLIC_VIEW_BASE_URL"],
    BearerToken = builder.Configuration["BrowserRuntime:BearerToken"]
        ?? builder.Configuration["BROWSER_SERVICE_TOKEN"],
    TimeoutSeconds = int.TryParse(
        builder.Configuration["BrowserRuntime:TimeoutSeconds"]
        ?? builder.Configuration["BROWSER_TIMEOUT_SECONDS"],
        out var browserTimeout)
        ? browserTimeout
        : 30,
    Enabled = !string.Equals(
        builder.Configuration["BrowserRuntime:Enabled"]
        ?? builder.Configuration["BROWSER_ENABLED"],
        "false",
        StringComparison.OrdinalIgnoreCase),
};
builder.Services.AddSingleton(browserRuntimeConfig);

// PostHog
var postHogConfig = RequireSection<PostHogConfig>("PostHog");
builder.Services.AddSingleton(postHogConfig);

// GraphQL — dashboard operator API
builder.Services.AddHttpContextAccessor();

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

app.MapGraphQL("/api/dashboard/graphql", schemaName: "dashboard")
    .RequireCors(FrontendCorsPolicy);
app.MapControllers();
app.Run();

public partial class Program { }
