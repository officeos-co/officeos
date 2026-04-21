namespace EnterpriseAgentOs.Infrastructure.Common.Configuration;

public static class ValueManager
{
    private static IConfiguration? _configuration;
    private static string? _environmentName;

    /// <summary>
    /// Override the configuration source. Used by tests to inject
    /// WebApplicationFactory configuration (which includes UseSetting overrides).
    /// </summary>
    public static void SetConfiguration(IConfiguration configuration)
    {
        _configuration = configuration;
        _environmentName = null; // reset so it re-reads from new config
    }

    private static IConfiguration Configuration
    {
        get
        {
            if (_configuration == null)
            {
                var builder = new ConfigurationBuilder()
                    .SetBasePath(Directory.GetCurrentDirectory())
                    .AddJsonFile("appsettings.json", optional: false)
                    .AddEnvironmentVariables();
                _configuration = builder.Build();
            }
            return _configuration;
        }
    }

    private static string EnvironmentName
    {
        get
        {
            if (_environmentName == null)
            {
                var env = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT");
                _environmentName = (env == "Production" || string.IsNullOrEmpty(env)) ? "Production" : "Staging";
            }
            return _environmentName;
        }
    }

    /// <summary>
    /// Returns the active environment name ("Production" or "Staging").
    /// Used in Program.cs to bind nested config sections.
    /// </summary>
    public static string GetEnvironmentName() => EnvironmentName;

    /// <summary>
    /// Exposes the underlying IConfiguration instance.
    /// Use only in Program.cs to bind nested config sections (e.g. Stripe).
    /// </summary>
    public static IConfiguration GetConfiguration() => Configuration;

    public static T GetValue<T>(string key)
    {
        var value = Configuration.GetValue<T>($"{EnvironmentName}:{key}");
        if (value == null)
        {
            throw new InvalidOperationException(
                $"Configuration value '{key}' not found in '{EnvironmentName}' section.");
        }
        return value;
    }
}
