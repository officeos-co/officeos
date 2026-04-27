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
builder.Services.AddMemoryCache();
builder.Services.AddStackExchangeRedisCache(options =>
{
    options.Configuration = ValueManager.GetValue<string>("Redis");
    options.InstanceName = "eaos:";
});

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

// DI — each layer registers its own services
builder.Services.AddInfrastructure(ValueManager.GetValue<string>("ConnectionString"));
builder.Services.AddApplication();
builder.Services.AddMediatR(cfg => cfg.RegisterServicesFromAssemblies(
    typeof(EnterpriseAgentOs.Application.ApplicationServiceRegistration).Assembly,
    typeof(Program).Assembly));

// Infrastructure configs — bind from nested appsettings sections
var envSection = ValueManager.GetConfiguration().GetSection(ValueManager.GetEnvironmentName());

var kubernetesConfig = new KubernetesConfig();
envSection.GetSection("Kubernetes").Bind(kubernetesConfig);
builder.Services.AddSingleton(kubernetesConfig);

var skillGatewayConfig = new SkillGatewayConfig { RefreshSeconds = 30 };
envSection.GetSection("SkillGateway").Bind(skillGatewayConfig);
builder.Services.AddSingleton(skillGatewayConfig);

var skillRuntimeConfig = new SkillRuntimeConfig();
envSection.GetSection("SkillRuntime").Bind(skillRuntimeConfig);
builder.Services.AddSingleton(skillRuntimeConfig);

var googleOAuthConfig = new GoogleOAuthConfig();
envSection.GetSection("GoogleOAuth").Bind(googleOAuthConfig);
builder.Services.AddSingleton(googleOAuthConfig);

var skillStorageConfig = new SkillStorageConfig();
envSection.GetSection("Minio").Bind(skillStorageConfig);
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
builder.Services.AddSingleton(envSection.GetSection("PlatformKeys").Get<PlatformKeysConfig>() ?? new PlatformKeysConfig());

// Session auth — configurable skip prefixes
var sessionAuthConfig = new SessionAuthConfig();
envSection.GetSection("SessionAuth").Bind(sessionAuthConfig);
builder.Services.AddSingleton(sessionAuthConfig);

// PostHog — server owns the API key; dashboard calls use-case-specific track* mutations
var postHogConfig = new PostHogConfig();
envSection.GetSection("PostHog").Bind(postHogConfig);
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
