using k8s;
using Microsoft.AspNetCore.DataProtection;

const string FrontendCorsPolicy = "v2-frontend";

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var dpKeyPath = ValueManager.GetValue<string>("DataProtectionKeyPath");
var dpKeyDir = System.IO.Path.IsPathRooted(dpKeyPath)
    ? dpKeyPath
    : System.IO.Path.Combine(Directory.GetCurrentDirectory(), dpKeyPath);
Directory.CreateDirectory(dpKeyDir);
builder.Services
    .AddDataProtection()
    .PersistKeysToFileSystem(new DirectoryInfo(dpKeyDir))
    .SetApplicationName("EnterpriseAgentOs.Api");

builder.Services.AddSingleton<ProviderKeyProtector>();
builder.Services.AddSingleton<SkillCredentialProtector>();

builder.Services.AddDbContext<EaosDbContext>(options =>
    options.UseNpgsql(ValueManager.GetValue<string>("ConnectionString")));

builder.Services.AddScoped<IAgentRepository, AgentRepository>();
builder.Services.AddScoped<IAgentService, AgentService>();
builder.Services.AddHttpClient<IVaultClient, CouchDbVaultClient>();
builder.Services.AddHttpClient("agent-proxy");

var kubernetesConfig = new KubernetesConfig
{
    Enabled = ValueManager.GetValue<bool>("KubernetesEnabled"),
    Namespace = ValueManager.GetValue<string>("KubernetesNamespace"),
    Image = ValueManager.GetValue<string>("ZeroclawImage"),
};
builder.Services.AddSingleton(kubernetesConfig);

var couchDbConfig = new CouchDbConfig
{
    Url = ValueManager.GetValue<string>("CouchDbUrl"),
    User = ValueManager.GetValue<string>("CouchDbUser"),
    Password = ValueManager.GetValue<string>("CouchDbPassword"),
};
builder.Services.AddSingleton(couchDbConfig);

var skillGatewayConfig = new SkillGatewayConfig
{
    Url = ValueManager.GetValue<string>("SkillGatewayUrl"),
    RefreshSeconds = 30,
};
builder.Services.AddSingleton(skillGatewayConfig);

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
builder.Services.AddScoped<IProviderRepository, ProviderRepository>();
builder.Services.AddScoped<IProviderService, ProviderService>();
builder.Services.AddScoped<ISkillCredentialRepository, SkillCredentialRepository>();
builder.Services.AddScoped<ISkillService, SkillService>();

var skillRuntimeConfig = new SkillRuntimeConfig
{
    Url = ValueManager.GetValue<string>("SkillRuntimeUrl"),
};
builder.Services.AddSingleton(skillRuntimeConfig);
builder.Services.AddHttpClient<SkillRuntimeClient>();

var googleOAuthConfig = new GoogleOAuthConfig
{
    ClientId = ValueManager.GetValue<string>("GoogleOAuthClientId"),
    ClientSecret = ValueManager.GetValue<string>("GoogleOAuthClientSecret"),
    RedirectUri = ValueManager.GetValue<string>("GoogleOAuthRedirectUri"),
};
builder.Services.AddSingleton(googleOAuthConfig);

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

// Auth
builder.Services.AddScoped<IUserRepository, UserRepository>();
builder.Services.AddScoped<ISessionRepository, SessionRepository>();

// Runners
builder.Services.AddScoped<IRunnerRepository, RunnerRepository>();
builder.Services.AddScoped<IRunnerJobRepository, RunnerJobRepository>();
builder.Services.AddSingleton<RunnerJobWaiter>();
builder.Services.AddHostedService<RunnerJobTimeoutService>();

// Custom Skills
builder.Services.AddScoped<ICustomSkillRepository, CustomSkillRepository>();

builder.Services.AddHttpClient("llm-proxy");
builder.Services.AddScoped<LlmProviderDispatcher>();

builder.Services.AddSingleton<EnterpriseAgentOs.Api.Entities.Skills.GraphQL.SkillTypeModule>();
builder.Services
    .AddGraphQLServer()
    .AddQueryType<EnterpriseAgentOs.Api.Entities.Skills.GraphQL.Query>()
    .AddTypeModule<EnterpriseAgentOs.Api.Entities.Skills.GraphQL.SkillTypeModule>()
    .AddHttpRequestInterceptor<EnterpriseAgentOs.Api.Entities.Skills.GraphQL.AgentAuthInterceptor>()
    .DisableIntrospection(false)
    .SetIntrospectionAllowedDepth(20, 20);

builder.Services.AddCors(options =>
{
    options.AddPolicy(FrontendCorsPolicy, policy =>
    {
        policy
            .WithOrigins(ValueManager.GetValue<string>("FrontendOrigin"))
            .AllowAnyHeader()
            .AllowAnyMethod();
    });
});

var app = builder.Build();

using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<EaosDbContext>();
    await db.Database.EnsureCreatedAsync();
    await SeedProvidersAsync(db);
}

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

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
        new ProviderRecord { Name = "groq", DisplayName = "Groq" },
        new ProviderRecord { Name = "deepseek", DisplayName = "DeepSeek" },
        new ProviderRecord { Name = "openrouter", DisplayName = "OpenRouter" },
        new ProviderRecord { Name = "ollama", DisplayName = "Ollama" },
    };

    await db.Providers.AddRangeAsync(seed);
    await db.SaveChangesAsync();
}
