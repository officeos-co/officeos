using Microsoft.EntityFrameworkCore.Design;

namespace EnterpriseAgentOs.Infrastructure.Common;

/// <summary>
/// Design-time factory so EF Core tools can create a DbContext without a running ASP.NET host.
/// Used only by `dotnet ef migrations add` — never instantiated in production.
/// </summary>
public sealed class DesignTimeDbContextFactory : IDesignTimeDbContextFactory<EaosDbContext>
{
    public EaosDbContext CreateDbContext(string[] args)
    {
        var config = new ConfigurationBuilder()
            .SetBasePath(Directory.GetCurrentDirectory())
            .AddJsonFile("appsettings.json", optional: true)
            .AddEnvironmentVariables()
            .Build();

        var connectionString = config["ConnectionString"]
            ?? "Host=localhost;Port=5432;Database=eaos;Username=postgres;Password=postgres";

        var optionsBuilder = new DbContextOptionsBuilder<EaosDbContext>();
        optionsBuilder.UseNpgsql(connectionString);

        return new EaosDbContext(optionsBuilder.Options);
    }
}
