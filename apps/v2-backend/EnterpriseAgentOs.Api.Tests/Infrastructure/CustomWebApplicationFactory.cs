using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Testcontainers.PostgreSql;
using WireMock.Server;
using WireMock.RequestBuilders;
using WireMock.ResponseBuilders;
using EnterpriseAgentOs.Api.Database;
using EnterpriseAgentOs.Api.Entities.Agents;
using EnterpriseAgentOs.Api.Entities.Vault;
using EnterpriseAgentOs.Api.Properties;

namespace EnterpriseAgentOs.Api.Tests.Infrastructure;

public sealed class CustomWebApplicationFactory : WebApplicationFactory<Program>, IAsyncLifetime
{
    private readonly PostgreSqlContainer _postgres = new PostgreSqlBuilder()
        .WithImage("postgres:16-alpine")
        .Build();

    public WireMockServer SkillRuntimeMock { get; } = WireMockServer.Start();

    public string PostgresConnectionString => _postgres.GetConnectionString();

    public async Task InitializeAsync()
    {
        await _postgres.StartAsync();

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
        await _postgres.DisposeAsync();
    }

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        // Force Staging environment so ValueManager reads Staging section
        builder.UseEnvironment("Staging");

        builder.UseSetting("Staging:ConnectionString", _postgres.GetConnectionString());
        builder.UseSetting("Staging:KubernetesEnabled", "false");
        builder.UseSetting("Staging:SkillRuntimeUrl", SkillRuntimeMock.Url!);
        builder.UseSetting("Staging:SkillGatewayUrl", SkillRuntimeMock.Url!);
        builder.UseSetting("Staging:FrontendOrigin", "http://localhost:5173");
        builder.UseSetting("Staging:DataProtectionKeyPath", Path.Combine(Path.GetTempPath(), $"dp-keys-{Guid.NewGuid():N}"));
        builder.UseSetting("Staging:CouchDbUrl", "http://localhost:5984");
        builder.UseSetting("Staging:CouchDbUser", "test");
        builder.UseSetting("Staging:CouchDbPassword", "test");
        builder.UseSetting("Staging:GoogleOAuthClientId", "test-client-id");
        builder.UseSetting("Staging:GoogleOAuthClientSecret", "test-client-secret");
        builder.UseSetting("Staging:GoogleOAuthRedirectUri", "http://localhost/api/auth/callback/google");
        builder.UseSetting("Staging:MinioEndpoint", "http://localhost:9000");
        builder.UseSetting("Staging:MinioAccessKey", "testkey");
        builder.UseSetting("Staging:MinioSecretKey", "testsecret");
        builder.UseSetting("Staging:MinioBucket", "test-skills");

        builder.ConfigureServices(services =>
        {
            // Replace EF Core to use Testcontainers Postgres
            services.RemoveAll<DbContextOptions<EaosDbContext>>();
            services.RemoveAll<EaosDbContext>();
            services.AddDbContext<EaosDbContext>(options =>
                options.UseNpgsql(_postgres.GetConnectionString()));

            // Replace vault client with a no-op stub
            services.RemoveAll<IVaultClient>();
            services.AddScoped<IVaultClient, StubVaultClient>();

            // Replace SkillRuntimeConfig to point at WireMock
            services.RemoveAll<SkillRuntimeConfig>();
            services.AddSingleton(new SkillRuntimeConfig { Url = SkillRuntimeMock.Url! });
        });
    }
}
