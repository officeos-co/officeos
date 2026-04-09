using k8s;
using Microsoft.AspNetCore.DataProtection;

const string FrontendCorsPolicy = "v2-frontend";

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var dpKeyPath = ValueManager.GetValue<string>("DataProtectionKeyPath");
var dpKeyDir = Path.IsPathRooted(dpKeyPath)
    ? dpKeyPath
    : Path.Combine(Directory.GetCurrentDirectory(), dpKeyPath);
Directory.CreateDirectory(dpKeyDir);
builder.Services
    .AddDataProtection()
    .PersistKeysToFileSystem(new DirectoryInfo(dpKeyDir))
    .SetApplicationName("EnterpriseAgentOs.Api");

builder.Services.AddSingleton<ProviderKeyProtector>();

builder.Services.AddDbContext<EaosDbContext>(options =>
    options.UseSqlite(ValueManager.GetValue<string>("ConnectionString")));

builder.Services.AddScoped<IAgentRepository, AgentRepository>();
builder.Services.AddScoped<IAgentService, AgentService>();
builder.Services.AddHttpClient<IVaultClient, CouchDbVaultClient>();

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
builder.Services.AddScoped<ISkillRepository, SkillRepository>();
builder.Services.AddScoped<ISkillService, SkillService>();

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

app.UseWebSockets();

app.MapGet("/api/health", () => Results.Ok(new { ok = true }));
app.MapGet("/healthz", () => Results.Ok(new { ok = true }));

app.MapAgentProxyEndpoints();
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
