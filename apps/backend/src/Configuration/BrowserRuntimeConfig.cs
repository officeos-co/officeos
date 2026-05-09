namespace EnterpriseAgentOs.Configuration;

public sealed class BrowserRuntimeConfig
{
    public string BaseUrl { get; init; } = "http://browser:8000";
    public string? PublicViewBaseUrl { get; init; }
    public string? BearerToken { get; init; }
    public int TimeoutSeconds { get; init; } = 30;
    public bool Enabled { get; init; } = true;
}
