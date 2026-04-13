using k8s;
using Microsoft.AspNetCore.DataProtection;
using Serilog;
using Serilog.Events;
using EnterpriseAgentOs.Api.Middleware;
using EnterpriseAgentOs.Api.Extensions;

const string FrontendCorsPolicy = "v2-frontend";

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

// Infrastructure configs
builder.Services.AddSingleton(new KubernetesConfig
{
    Enabled = ValueManager.GetValue<bool>("KubernetesEnabled"),
    Namespace = ValueManager.GetValue<string>("KubernetesNamespace"),
    Image = ValueManager.GetValue<string>("ZeroclawImage"),
});

builder.Services.AddSingleton(new CouchDbConfig
{
    Url = ValueManager.GetValue<string>("CouchDbUrl"),
    User = ValueManager.GetValue<string>("CouchDbUser"),
    Password = ValueManager.GetValue<string>("CouchDbPassword"),
});

builder.Services.AddSingleton(new SkillGatewayConfig
{
    Url = ValueManager.GetValue<string>("SkillGatewayUrl"),
    RefreshSeconds = 30,
});

builder.Services.AddSingleton(new SkillRuntimeConfig
{
    Url = ValueManager.GetValue<string>("SkillRuntimeUrl"),
});

builder.Services.AddSingleton(new GoogleOAuthConfig
{
    ClientId = ValueManager.GetValue<string>("GoogleOAuthClientId"),
    ClientSecret = ValueManager.GetValue<string>("GoogleOAuthClientSecret"),
    RedirectUri = ValueManager.GetValue<string>("GoogleOAuthRedirectUri"),
});

builder.Services.AddSingleton(new WorkOsConfig
{
    ApiKey = ValueManager.GetValue<string>("WorkOsApiKey"),
    ClientId = ValueManager.GetValue<string>("WorkOsClientId"),
    RedirectUri = ValueManager.GetValue<string>("WorkOsRedirectUri"),
    Enabled = ValueManager.GetValue<bool>("WorkOsEnabled"),
});

var skillStorageConfig = new SkillStorageConfig
{
    Endpoint = ValueManager.GetValue<string>("MinioEndpoint"),
    AccessKey = ValueManager.GetValue<string>("MinioAccessKey"),
    SecretKey = ValueManager.GetValue<string>("MinioSecretKey"),
    Bucket = ValueManager.GetValue<string>("MinioBucket"),
};
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
var kubernetesEnabled = ValueManager.GetValue<bool>("KubernetesEnabled");
if (kubernetesEnabled)
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
ValueManager.GetConfiguration().GetSection($"{ValueManager.GetEnvironmentName()}:Stripe").Bind(stripeConfig);
builder.Services.AddSingleton(stripeConfig);

var frontendConfig = new FrontendConfig(ValueManager.GetValue<string>("FrontendOrigin"));
builder.Services.AddSingleton(frontendConfig);

// LLM
var env = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT");
var configSection = (env == "Production" || string.IsNullOrEmpty(env)) ? "Production" : "Staging";

builder.Services.AddSingleton(
    builder.Configuration.GetSection($"{configSection}:LiteLlm").Get<LiteLlmConfig>() ?? new LiteLlmConfig());
builder.Services.AddSingleton(
    builder.Configuration.GetSection($"{configSection}:PlatformKeys").Get<PlatformKeysConfig>() ?? new PlatformKeysConfig());

// GraphQL skill gateway
builder.Services.AddSingleton<EnterpriseAgentOs.Api.Entities.Skills.GraphQL.SkillTypeModule>();
builder.Services
    .AddGraphQLServer()
    .AddQueryType<EnterpriseAgentOs.Api.Entities.Skills.GraphQL.Query>()
    .AddTypeModule<EnterpriseAgentOs.Api.Entities.Skills.GraphQL.SkillTypeModule>()
    .AddHttpRequestInterceptor<EnterpriseAgentOs.Api.Entities.Skills.GraphQL.AgentAuthInterceptor>()
    .DisableIntrospection(false)
    .SetIntrospectionAllowedDepth(20, 20);

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
    await SeedProvidersAsync(db);
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
app.MapGraphQL("/api/graphql");
app.MapControllers();

app.Run();

static async Task SeedProvidersAsync(EaosDbContext db)
{
    if (await db.Providers.AnyAsync())
    {
        return;
    }

    var seed = new[]
    {
        new ProviderRecord { Name = "openai", DisplayName = "OpenAI" },
        new ProviderRecord { Name = "anthropic", DisplayName = "Anthropic" },
        new ProviderRecord { Name = "google", DisplayName = "Google Gemini" },
        new ProviderRecord { Name = "xai", DisplayName = "xAI Grok" },
    };

    await db.Providers.AddRangeAsync(seed);
    await db.SaveChangesAsync();
}

// Make Program visible to test project (WebApplicationFactory<Program>)
public partial class Program { }
