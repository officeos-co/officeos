using k8s;
using Microsoft.AspNetCore.DataProtection;
using Serilog;
using Serilog.Events;
using EnterpriseAgentOs.Api.Middleware;
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
    await SeedBuiltinSkillsAsync(scope.ServiceProvider);
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

static async Task SeedBuiltinSkillsAsync(IServiceProvider services)
{
    var logger = services.GetRequiredService<ILoggerFactory>().CreateLogger("SkillSeeder");
    var runtime = services.GetRequiredService<SkillRuntimeClient>();
    var db = services.GetRequiredService<EaosDbContext>();

    IReadOnlyList<RuntimeManifest> manifests;
    try
    {
        manifests = await runtime.GetManifestsAsync();
    }
    catch (Exception ex)
    {
        logger.LogWarning(ex, "Could not reach skill-runtime for seeding — builtin skills will be seeded on next startup");
        return;
    }

    if (manifests.Count == 0)
    {
        logger.LogWarning("Skill-runtime returned 0 manifests — skipping seed");
        return;
    }

    var systemSkills = new HashSet<string>(StringComparer.OrdinalIgnoreCase) { "browser" };
    var jsonOptions = new System.Text.Json.JsonSerializerOptions
    {
        PropertyNamingPolicy = System.Text.Json.JsonNamingPolicy.CamelCase,
    };

    foreach (var manifest in manifests)
    {
        var name = manifest.Name.Trim().ToLowerInvariant();
        var existing = await db.Skills.FirstOrDefaultAsync(s => s.Name == name);

        var manifestJson = System.Text.Json.JsonSerializer.Serialize(manifest, jsonOptions);

        if (existing is null)
        {
            db.Skills.Add(new SkillRecord
            {
                Name = name,
                Title = manifest.Title,
                Description = manifest.Description,
                Emoji = manifest.Emoji,
                Doc = manifest.Doc,
                Source = "builtin",
                ManifestJson = manifestJson,
                IsSystem = systemSkills.Contains(name),
                Status = "active",
            });
            logger.LogInformation("Seeded builtin skill: {SkillName}", name);
        }
        else if (existing.Source == "builtin")
        {
            // Update manifest for builtin skills on every startup to stay in sync
            existing.Title = manifest.Title;
            existing.Description = manifest.Description;
            existing.Emoji = manifest.Emoji;
            existing.Doc = manifest.Doc;
            existing.ManifestJson = manifestJson;
            existing.IsSystem = systemSkills.Contains(name);
            existing.UpdatedAt = DateTime.UtcNow;
            logger.LogDebug("Updated builtin skill manifest: {SkillName}", name);
        }
    }

    await db.SaveChangesAsync();
    logger.LogInformation("Skill seeding complete — {Count} builtin skills", manifests.Count);
}

// Make Program visible to test project (WebApplicationFactory<Program>)
public partial class Program { }
