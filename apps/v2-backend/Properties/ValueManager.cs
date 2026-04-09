namespace EnterpriseAgentOs.Api.Properties;

public static class ValueManager
{
    private static IConfiguration? _configuration;
    private static string? _environmentName;

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
